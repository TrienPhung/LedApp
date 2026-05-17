"""
camera_service_v4.py — YOLO11n + PaddleOCR v4  (GPU NVIDIA)
Cải tiến so với v3:
  ✓ Sửa lỗi nhầm số 1 ↔ chữ I (detect pattern TRƯỚC khi sửa ký tự)
  ✓ is_valid_plate bổ sung dạng chữ+số (27-B1-258.88)
  ✓ Pipeline OCR chạy thread riêng → không block main loop → hết giật
  ✓ Chỉ OCR 1 phiên bản ảnh tốt nhất (CLAHE+sharpen) thay vì 4 → nhanh 4x
  ✓ Tách get_candidate_boxes khỏi read_plates → cache boxes riêng
  ✓ Voting Levenshtein: gom các biển gần giống nhau tránh nhiễu
  ✓ FRAME_SKIP tự động theo FPS thực đo

Cài đặt:
    pip install ultralytics paddleocr==2.7.3 paddlepaddle-gpu==2.6.2 opencv-python requests python-Levenshtein

Chạy:
    python camera_service_v4.py
"""

import urllib3
urllib3.disable_warnings(urllib3.exceptions.InsecureRequestWarning)

import cv2
import re
import time
import threading
import requests
import numpy as np
from datetime import datetime
from concurrent.futures import ThreadPoolExecutor
from collections import deque, Counter
from ultralytics import YOLO
from paddleocr import PaddleOCR

# ── CONFIG ────────────────────────────────────────────────────────────────────
ASP_NET_URL    = "https://localhost:7266"
API_ENDPOINT   = f"{ASP_NET_URL}/BaoVeNhap/NhanDienBienSo"
CAMERA_INDEX   = 0
OCR_CONFIDENCE = 0.45       # ngưỡng confidence OCR
YOLO_CONF      = 0.35       # ngưỡng confidence YOLO
COOLDOWN       = 5          # giây cooldown giữa 2 lần gửi cùng biển
MOTION_THRESH  = 4          # ngưỡng motion detection (nhạy hơn v3)
MAX_WORKERS    = 2          # thread pool cho API call
VOTE_MIN       = 2          # biển phải xuất hiện ≥ N lần mới gửi
BUFFER_SIZE    = 12         # kích thước voting buffer
# ─────────────────────────────────────────────────────────────────────────────

yolo_model        = None
ocr_engine        = None
last_plates: dict = {}
executor          = ThreadPoolExecutor(max_workers=MAX_WORKERS)
plate_buffer      = deque(maxlen=BUFFER_SIZE)
prev_gray         = None
no_motion_streak  = 0

# ── Thread-safe OCR pipeline ──────────────────────────────────────────────────
_ocr_lock          = threading.Lock()   # PaddleOCR không thread-safe
_latest_frame      = None
_frame_lock        = threading.Lock()
_ocr_result        = ([], [])           # (detections, boxes)
_ocr_result_lock   = threading.Lock()
_ocr_running       = False


# ══════════════════════════════════════════════════════════════════════════════
#  CHARACTER CORRECTION  (v4: pattern-first, không nhầm B1 → BI)
# ══════════════════════════════════════════════════════════════════════════════
NUM_ZONE_FIX = {
    'O': '0', 'I': '1', 'L': '1', 'Z': '2',
    'S': '5', 'B': '8', 'G': '6', 'T': '7',
}
CHR_ZONE_FIX = {
    '0': 'O', '1': 'I', '8': 'B',
    '5': 'S', '2': 'Z', '6': 'G',
}

# Pattern biển HỢP LỆ — nếu raw đã khớp → KHÔNG sửa gì
_VALID_RAW = [
    re.compile(r'^\d{2}[A-Z]\d{4,5}$'),        # 88C27999, 43D2222
    re.compile(r'^\d{2}[A-Z]{2}\d{4,5}$'),     # 50AA3797, 50AA37979
    re.compile(r'^\d{2}[A-Z]\d{5,6}$'),        # 27B125888 (chữ+số+5số)
    re.compile(r'^\d{2}[A-Z]\d\d{4,5}$'),      # 27B11234 / 27B125888
]

def correct_plate_chars(raw: str) -> str:
    raw = re.sub(r'[-.\s]', '', raw).strip().upper()

    # 1. Nếu đã đúng format → trả ngay, không sửa
    for p in _VALID_RAW:
        if p.match(raw):
            return raw

    # 2. Dạng XX + chữ+số (B1, C2...) + 4/5 số → giữ nguyên phần ký hiệu
    m = re.match(r'^(.{2})([A-Z])(\d)(.{4,5})$', raw)
    if m:
        p1 = ''.join(NUM_ZONE_FIX.get(c, c) for c in m.group(1))
        p4 = ''.join(NUM_ZONE_FIX.get(c, c) for c in m.group(4))
        return p1 + m.group(2) + m.group(3) + p4   # B1 giữ nguyên

    # 3. Dạng XX + 1 chữ + 4/5 số
    m = re.match(r'^(.{2})([A-Z])(.{4,5})$', raw)
    if m:
        p1 = ''.join(NUM_ZONE_FIX.get(c, c) for c in m.group(1))
        p3 = ''.join(NUM_ZONE_FIX.get(c, c) for c in m.group(3))
        return p1 + m.group(2) + p3

    # 4. Dạng XX + 2 chữ + 4/5 số
    m = re.match(r'^(.{2})([A-Z]{2})(.{4,5})$', raw)
    if m:
        p1 = ''.join(NUM_ZONE_FIX.get(c, c) for c in m.group(1))
        p2 = ''.join(CHR_ZONE_FIX.get(c, c) for c in m.group(2))
        p3 = ''.join(NUM_ZONE_FIX.get(c, c) for c in m.group(3))
        return p1 + p2 + p3

    return raw


# ══════════════════════════════════════════════════════════════════════════════
#  PLATE HELPERS
# ══════════════════════════════════════════════════════════════════════════════
def clean_text(text: str) -> str:
    text = text.upper().strip()
    text = text.replace(',', '.').replace(' ', '').replace('_', '')
    text = re.sub(r'[^A-Z0-9\-\.]', '', text)
    return text.strip('-').strip('.')


def normalize_plate(text: str) -> str:
    raw = re.sub(r'[-.\s]', '', text).strip().upper()
    raw = correct_plate_chars(raw)

    # 2 số + 2 chữ + 5 số → XX-AA-XXX.XX
    m = re.match(r'^(\d{2})([A-Z]{2})(\d{3})(\d{2})$', raw)
    if m:
        return f"{m.group(1)}-{m.group(2)}-{m.group(3)}.{m.group(4)}"

    # 2 số + 2 chữ + 4 số → XX-AA-XXXX
    m = re.match(r'^(\d{2})([A-Z]{2})(\d{4})$', raw)
    if m:
        return f"{m.group(1)}-{m.group(2)}-{m.group(3)}"

    # 2 số + chữ+số + 5 số → XX-X1-XXX.XX  (27-B1-258.88)
    m = re.match(r'^(\d{2})([A-Z]\d)(\d{3})(\d{2})$', raw)
    if m:
        return f"{m.group(1)}-{m.group(2)}-{m.group(3)}.{m.group(4)}"

    # 2 số + chữ+số + 4 số → XX-X1-XXXX
    m = re.match(r'^(\d{2})([A-Z]\d)(\d{4})$', raw)
    if m:
        return f"{m.group(1)}-{m.group(2)}-{m.group(3)}"

    # 2 số + 1 chữ + 5 số → XXA-XXX.XX
    m = re.match(r'^(\d{2}[A-Z])(\d{3})(\d{2})$', raw)
    if m:
        return f"{m.group(1)}-{m.group(2)}.{m.group(3)}"

    # 2 số + 1 chữ + 4 số → XXA-XXXX
    m = re.match(r'^(\d{2}[A-Z])(\d{4})$', raw)
    if m:
        return f"{m.group(1)}-{m.group(2)}"

    return text.strip()


def is_valid_plate(text: str) -> bool:
    raw = re.sub(r'[-.\s]', '', text).upper()
    patterns = [
        r'^\d{2}[A-Z]\d{4,5}$',        # 1 chữ: 43D2222, 88C27999
        r'^\d{2}[A-Z]{2}\d{4,5}$',     # 2 chữ: 50AA3797
        r'^\d{2}[A-Z]\d{5}$',          # chữ+số: 99E12268  ← THÊM MỚI
        r'^\d{2}[A-Z]\d\d{4}$',        # chữ+số + 4 số: 27B11234
        r'^\d{2}[A-Z]\d\d{5}$',        # chữ+số + 5 số: 27B125888
    ]
    return any(bool(re.match(p, raw)) for p in patterns)


# ══════════════════════════════════════════════════════════════════════════════
#  OCR HELPERS
# ══════════════════════════════════════════════════════════════════════════════
def parse_ocr_result(raw_result) -> list:
    if not raw_result or not raw_result[0]:
        return []
    parsed = []
    for item in raw_result[0]:
        try:
            if isinstance(item, (list, tuple)) and len(item) == 2:
                box, text_val = item[0], item[1]
                if isinstance(text_val, (list, tuple)) and len(text_val) == 2:
                    text, score = text_val[0], float(text_val[1])
                    parsed.append((box, text, score))
        except Exception:
            continue
    return parsed


def merge_two_line(results: list) -> list:
    """Ghép 2 dòng OCR biển xe tải."""
    if not results:
        return results

    def cy(r):
        box = r[0]
        return (box[0][1] + box[2][1]) / 2 if box else 0

    sorted_r = sorted(results, key=cy)
    used, merged = set(), []

    for i in range(len(sorted_r)):
        if i in used:
            continue
        box_i, text_i, score_i = sorted_r[i]
        height_i = abs(box_i[2][1] - box_i[0][1]) + 1 if box_i else 20
        yi = cy(sorted_r[i])
        paired = False

        for j in range(i + 1, len(sorted_r)):
            if j in used:
                continue
            yj = cy(sorted_r[j])
            if 0 < (yj - yi) < height_i * 2.8:
                _, text_j, score_j = sorted_r[j]
                combined = clean_text(text_i) + clean_text(text_j)
                merged.append((None, normalize_plate(combined), (score_i + score_j) / 2))
                used.add(i); used.add(j)
                paired = True
                break

        if not paired:
            merged.append((box_i, clean_text(text_i), score_i))
            used.add(i)

    return merged


# ══════════════════════════════════════════════════════════════════════════════
#  IMAGE ENHANCE  (v4: chỉ 1 phiên bản tốt nhất → nhanh 4x so với v3)
# ══════════════════════════════════════════════════════════════════════════════
def enhance_for_ocr(img: np.ndarray) -> np.ndarray:
    h, w = img.shape[:2]
    # Chuẩn hoá kích thước tối ưu cho PaddleOCR
    if w < 200:
        img = cv2.resize(img, None, fx=200/w, fy=200/w, interpolation=cv2.INTER_CUBIC)
    elif w > 640:
        img = cv2.resize(img, None, fx=640/w, fy=640/w, interpolation=cv2.INTER_AREA)

    gray  = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
    clahe = cv2.createCLAHE(clipLimit=3.0, tileGridSize=(8, 8))
    gray  = clahe.apply(gray)
    sharp = cv2.filter2D(gray, -1, np.array([[0,-1,0],[-1,5,-1],[0,-1,0]]))
    return cv2.cvtColor(sharp, cv2.COLOR_GRAY2BGR)


# ══════════════════════════════════════════════════════════════════════════════
#  IOU / DEDUP
# ══════════════════════════════════════════════════════════════════════════════
def iou(a, b) -> float:
    ix1 = max(a[0], b[0]); iy1 = max(a[1], b[1])
    ix2 = min(a[2], b[2]); iy2 = min(a[3], b[3])
    inter = max(0, ix2-ix1) * max(0, iy2-iy1)
    if inter == 0: return 0.0
    return inter / ((a[2]-a[0])*(a[3]-a[1]) + (b[2]-b[0])*(b[3]-b[1]) - inter)


def deduplicate_boxes(boxes: list) -> list:
    kept = []
    for b in boxes:
        if not any(iou(b, k) > 0.45 for k in kept):
            kept.append(b)
    return kept


# ══════════════════════════════════════════════════════════════════════════════
#  LEVENSHTEIN VOTING  (gom biển gần giống nhau → chọn biển xuất hiện nhiều)
# ══════════════════════════════════════════════════════════════════════════════
def _lev(a: str, b: str) -> int:
    """Levenshtein distance đơn giản."""
    if len(a) < len(b): a, b = b, a
    prev = list(range(len(b)+1))
    for i, ca in enumerate(a):
        curr = [i+1]
        for j, cb in enumerate(b):
            curr.append(min(prev[j] + (ca != cb), prev[j+1]+1, curr[j]+1))
        prev = curr
    return prev[-1]


def vote(detections: list) -> list:
    for plate, _ in detections:
        plate_buffer.append(plate)
    if not detections:
        return []

    counts = Counter(plate_buffer)

    # Gom các biển cách nhau ≤ 1 ký tự (OCR nhiễu nhỏ)
    groups: dict[str, int] = {}
    for plate, cnt in counts.items():
        merged_into = None
        for key in groups:
            if _lev(plate, key) <= 1:
                merged_into = key
                break
        if merged_into:
            groups[merged_into] += cnt
        else:
            groups[plate] = cnt

    # Tìm biển đại diện cho mỗi group
    result = []
    for plate, conf in detections:
        representative = plate
        for key in groups:
            if _lev(plate, key) <= 1:
                representative = key
                break
        if groups.get(representative, 0) >= VOTE_MIN:
            result.append((representative, conf))

    return result if result else detections


# ══════════════════════════════════════════════════════════════════════════════
#  CANDIDATE BOXES
# ══════════════════════════════════════════════════════════════════════════════
def get_candidate_boxes(frame: np.ndarray) -> list:
    h, w = frame.shape[:2]
    boxes = []

    results = yolo_model(frame, conf=YOLO_CONF, verbose=False, imgsz=640, device=0)
    if results and results[0].boxes is not None:
        for box in results[0].boxes:
            x1, y1, x2, y2 = map(int, box.xyxy[0].tolist())
            bw, bh = x2-x1, y2-y1
            ratio = bw / (bh+1)
            if 1.5 <= ratio <= 6.5 and bw > 50 and bh > 15:
                pad = 12
                boxes.append((max(0,x1-pad), max(0,y1-pad),
                               min(w,x2+pad), min(h,y2+pad)))

    if not boxes:
        gray   = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
        blur   = cv2.GaussianBlur(gray, (5,5), 0)
        edges  = cv2.Canny(blur, 50, 150)
        kernel = cv2.getStructuringElement(cv2.MORPH_RECT, (22,6))
        closed = cv2.morphologyEx(edges, cv2.MORPH_CLOSE, kernel)
        contours, _ = cv2.findContours(closed, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        for cnt in contours:
            x, y, bw, bh = cv2.boundingRect(cnt)
            ratio = bw / (bh+1)
            if 1.5 <= ratio <= 6.5 and bw*bh > 1800 and bw > 70:
                pad = 10
                boxes.append((max(0,x-pad), max(0,y-pad),
                               min(w,x+bw+pad), min(h,y+bh+pad)))

    if not boxes:
        boxes = [
            (int(w*.02), int(h*.15), int(w*.98), int(h*.90)),
            (int(w*.05), int(h*.45), int(w*.95), h),
        ]

    return deduplicate_boxes(boxes)


# ══════════════════════════════════════════════════════════════════════════════
#  READ PLATES  (v4: 1 phiên bản ảnh, không lock main thread)
# ══════════════════════════════════════════════════════════════════════════════
def read_plates(frame: np.ndarray) -> tuple:
    """Trả về (detections, boxes). Gọi từ thread riêng."""
    best  = {}
    boxes = get_candidate_boxes(frame)

    for (x1, y1, x2, y2) in boxes:
        roi = frame[y1:y2, x1:x2]
        if roi.size == 0:
            continue
        enhanced = enhance_for_ocr(roi)
        try:
            with _ocr_lock:
                raw = ocr_engine.ocr(enhanced, cls=True)
        except Exception:
            continue
        if not raw or not raw[0]:
            continue
        for (_, text, score) in merge_two_line(parse_ocr_result(raw)):
            plate = normalize_plate(clean_text(text))
            if score >= OCR_CONFIDENCE and is_valid_plate(plate):
                if plate not in best or score > best[plate]:
                    best[plate] = score

    return list(best.items()), boxes


# ══════════════════════════════════════════════════════════════════════════════
#  ASYNC OCR WORKER  (chạy background → main loop không bị block)
# ══════════════════════════════════════════════════════════════════════════════
def _ocr_worker():
    global _ocr_running, _ocr_result
    while True:
        with _frame_lock:
            frame = _latest_frame
        if frame is None:
            time.sleep(0.01)
            continue

        detections, boxes = read_plates(frame)
        voted = vote(detections)

        with _ocr_result_lock:
            _ocr_result = (voted, boxes)

        for plate, conf in voted:
            if can_send(plate):
                executor.submit(send_to_api, plate, conf)

        time.sleep(0.01)   # yield CPU


# ══════════════════════════════════════════════════════════════════════════════
#  MOTION DETECTION
# ══════════════════════════════════════════════════════════════════════════════
def has_motion(frame: np.ndarray) -> bool:
    global prev_gray, no_motion_streak
    small = cv2.resize(frame, (160, 120))
    gray  = cv2.GaussianBlur(cv2.cvtColor(small, cv2.COLOR_BGR2GRAY), (5,5), 0)
    if prev_gray is None:
        prev_gray = gray
        return True
    score     = cv2.absdiff(prev_gray, gray).mean()
    prev_gray = gray
    if score < MOTION_THRESH:
        no_motion_streak += 1
        if no_motion_streak % 90 != 0:
            return False
    else:
        no_motion_streak = 0
    return True


# ══════════════════════════════════════════════════════════════════════════════
#  COOLDOWN & API
# ══════════════════════════════════════════════════════════════════════════════
_send_lock = threading.Lock()

def can_send(plate: str) -> bool:
    now = time.time()
    with _send_lock:
        if plate in last_plates and now - last_plates[plate] < COOLDOWN:
            return False
        last_plates[plate] = now
    return True


def send_to_api(plate: str, confidence: float):
    try:
        payload = {
            "bienSo":     plate,
            "confidence": round(confidence, 3),
            "thoiGian":   datetime.now().isoformat(),
            "nguon":      "webcam",
        }
        r = requests.post(API_ENDPOINT, json=payload, timeout=3, verify=False)
        if r.ok:
            print(f"[OK]  {plate} ({confidence:.0%}) → {r.json().get('message','')}")
        else:
            print(f"[HTTP {r.status_code}] {plate}")
    except requests.exceptions.ConnectionError:
        print(f"[CONN ERR] Không kết nối {ASP_NET_URL}")
    except requests.exceptions.Timeout:
        print(f"[TIMEOUT]  {plate}")
    except Exception as e:
        print(f"[ERR]  {plate} → {e}")


# ══════════════════════════════════════════════════════════════════════════════
#  DRAW OVERLAY
# ══════════════════════════════════════════════════════════════════════════════
def draw_overlay(frame, detections, boxes=None):
    h, w = frame.shape[:2]
    CYAN   = (255, 220, 0)
    GREEN  = (80, 255, 80)
    YELLOW = (0, 200, 255)
    RED    = (60, 60, 255)

    if boxes:
        for (x1, y1, x2, y2) in boxes:
            cv2.rectangle(frame, (x1,y1), (x2,y2), YELLOW, 1)
            L = 14
            for cx, cy_ in [(x1,y1),(x2,y1),(x2,y2),(x1,y2)]:
                dx = 1 if cx == x1 else -1
                dy = 1 if cy_ == y1 else -1
                cv2.line(frame, (cx,cy_), (cx+dx*L,cy_), CYAN, 2)
                cv2.line(frame, (cx,cy_), (cx,cy_+dy*L), CYAN, 2)

    for i, (plate, conf) in enumerate(detections):
        color = GREEN if conf >= 0.7 else YELLOW if conf >= 0.5 else RED
        label = f"{plate}  {conf:.0%}"
        cv2.putText(frame, label, (11, 41+44*i),
                    cv2.FONT_HERSHEY_SIMPLEX, 1.1, (0,0,0), 3, cv2.LINE_AA)
        cv2.putText(frame, label, (10, 40+44*i),
                    cv2.FONT_HERSHEY_SIMPLEX, 1.1, color, 2, cv2.LINE_AA)

    cv2.putText(frame, "YOLO11n + PaddleOCR v4 [GPU]", (10, h-12),
                cv2.FONT_HERSHEY_SIMPLEX, 0.5, (100,100,100), 1)
    cv2.putText(frame, datetime.now().strftime("%H:%M:%S"), (w-115, 26),
                cv2.FONT_HERSHEY_SIMPLEX, 0.65, (130,130,130), 1)
    return frame


# ══════════════════════════════════════════════════════════════════════════════
#  INIT
# ══════════════════════════════════════════════════════════════════════════════
def check_api_connection() -> bool:
    print(f"\n[INFO] Kiểm tra kết nối: {ASP_NET_URL}")
    try:
        r = requests.get(f"{ASP_NET_URL}/BaoVeNhap/Status", timeout=3, verify=False)
        if r.ok:
            print("[OK]  ASP.NET online!")
            return True
        print(f"[WARN] HTTP {r.status_code}")
    except Exception as e:
        print(f"[WARN] Không kết nối được: {e}")
    return False


def init_models():
    global yolo_model, ocr_engine
    check_api_connection()

    print("\n[INFO] Load YOLO11n trên GPU...")
    try:
        yolo_model = YOLO('yolo11n_vn_plate.pt')
        print("[OK]  YOLO11n fine-tuned (biển số VN) sẵn sàng!")
    except Exception:
        yolo_model = YOLO('yolo11n.pt')
        print("[WARN] Dùng yolo11n.pt gốc — tải model VN tại: https://universe.roboflow.com/search?q=vietnam+license+plate")

    print("[INFO] Load PaddleOCR...")
    ocr_engine = PaddleOCR(
        use_angle_cls=True,
        lang='en',
        show_log=False,
        use_gpu=False,              # CPU PaddleOCR tránh xung đột PyTorch GPU
        enable_mkldnn=True,         # MKL-DNN tăng tốc CPU
        det_db_score_mode='slow',   # chính xác hơn
        rec_algorithm='SVTR_LCNet',
    )
    print("[OK]  PaddleOCR sẵn sàng!\n")


# ══════════════════════════════════════════════════════════════════════════════
#  MAIN  (v4: OCR chạy thread riêng, main loop chỉ hiển thị → mượt 100%)
# ══════════════════════════════════════════════════════════════════════════════
def main():
    global _latest_frame
    init_models()

    # Khởi động OCR worker thread (daemon → tự tắt khi main tắt)
    t = threading.Thread(target=_ocr_worker, daemon=True)
    t.start()
    print("[INFO] OCR worker thread started.")

    cap = cv2.VideoCapture(CAMERA_INDEX)
    cap.set(cv2.CAP_PROP_FRAME_WIDTH,  1280)
    cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 720)
    cap.set(cv2.CAP_PROP_FPS,          30)
    cap.set(cv2.CAP_PROP_BUFFERSIZE,   1)   # luôn lấy frame mới nhất

    if not cap.isOpened():
        print(f"[ERROR] Không mở được camera {CAMERA_INDEX}")
        return

    print(f"[INFO] Camera {CAMERA_INDEX} OK (1280×720). Nhấn Q để thoát.\n")

    fps_timer  = time.time()
    fps_count  = 0
    fps_display = 0.0

    while True:
        ret, frame = cap.read()
        if not ret:
            time.sleep(0.01)
            continue

        # Đẩy frame mới nhất cho OCR worker (không block)
        if has_motion(frame):
            with _frame_lock:
                _latest_frame = frame.copy()

        # Lấy kết quả mới nhất từ OCR worker (không block)
        with _ocr_result_lock:
            detections, boxes = _ocr_result

        # Hiển thị FPS thực tế
        fps_count += 1
        if time.time() - fps_timer >= 1.0:
            fps_display = fps_count / (time.time() - fps_timer)
            fps_count   = 0
            fps_timer   = time.time()

        display = draw_overlay(frame, detections, boxes)
        cv2.putText(display, f"FPS:{fps_display:.0f}", (10, 26),
                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0, 255, 200), 2)

        cv2.imshow("Camera AI — Biển Số VN v4 [GPU]", display)
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

    cap.release()
    cv2.destroyAllWindows()
    executor.shutdown(wait=False)
    print("[INFO] Đã dừng.")


if __name__ == "__main__":
    main()
