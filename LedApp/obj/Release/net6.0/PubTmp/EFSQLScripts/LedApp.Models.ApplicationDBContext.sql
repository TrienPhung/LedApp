IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [CuaNhap] (
        [Id] int NOT NULL IDENTITY,
        [Ten] nvarchar(max) NOT NULL,
        [Mota] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CuaNhap] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [CuaXuat] (
        [Id] int NOT NULL IDENTITY,
        [Ten] nvarchar(max) NOT NULL,
        [Mota] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_CuaXuat] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [nguoiDungs] (
        [Id] uniqueidentifier NOT NULL,
        [Username] nvarchar(max) NOT NULL,
        [Password] nvarchar(max) NOT NULL,
        [Name] nvarchar(max) NOT NULL,
        [Image] nvarchar(max) NOT NULL,
        [Tels] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [Quyen] int NOT NULL,
        CONSTRAINT [PK_nguoiDungs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [Nhap] (
        [Id] int NOT NULL IDENTITY,
        [CuaNhapId] int NOT NULL,
        [TenCuaNhap] nvarchar(max) NOT NULL,
        [BienSoXe] nvarchar(max) NOT NULL,
        [NgayNhap] datetime2 NOT NULL,
        [GioNhap] int NOT NULL,
        [PhutNhap] int NOT NULL,
        [CongVao] int NOT NULL,
        [TrangThai] bit NOT NULL,
        CONSTRAINT [PK_Nhap] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Nhap_CuaNhap_CuaNhapId] FOREIGN KEY ([CuaNhapId]) REFERENCES [CuaNhap] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [dulieuxuat] (
        [Id] int NOT NULL IDENTITY,
        [CuaXuatId] int NOT NULL,
        [TenCuaXuat] nvarchar(max) NOT NULL,
        [BienSoXe] nvarchar(max) NOT NULL,
        [NgayXuat] datetime2 NOT NULL,
        [GioXuat] int NOT NULL,
        [PhutXuat] int NOT NULL,
        [CongRa] int NOT NULL,
        [TrangThai] bit NOT NULL,
        CONSTRAINT [PK_dulieuxuat] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_dulieuxuat_CuaXuat_CuaXuatId] FOREIGN KEY ([CuaXuatId]) REFERENCES [CuaXuat] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [ChitietNhap] (
        [Id] int NOT NULL IDENTITY,
        [NhapId] int NOT NULL,
        [BienSo] nvarchar(max) NOT NULL,
        [Soluong] bigint NOT NULL,
        [donvi] nvarchar(max) NOT NULL,
        CONSTRAINT [PK_ChitietNhap] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChitietNhap_Nhap_NhapId] FOREIGN KEY ([NhapId]) REFERENCES [Nhap] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE TABLE [ChitietXuat] (
        [Id] int NOT NULL IDENTITY,
        [TenCua] nvarchar(max) NOT NULL,
        [BienSo] nvarchar(max) NOT NULL,
        [SoluongCH] bigint NOT NULL,
        [donviCH] nvarchar(max) NOT NULL,
        [SoluongD] bigint NOT NULL,
        [donviD] nvarchar(max) NOT NULL,
        [dulieuxuatId] int NOT NULL,
        CONSTRAINT [PK_ChitietXuat] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ChitietXuat_dulieuxuat_dulieuxuatId] FOREIGN KEY ([dulieuxuatId]) REFERENCES [dulieuxuat] ([Id]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE INDEX [IX_ChitietNhap_NhapId] ON [ChitietNhap] ([NhapId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE INDEX [IX_ChitietXuat_dulieuxuatId] ON [ChitietXuat] ([dulieuxuatId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE INDEX [IX_dulieuxuat_CuaXuatId] ON [dulieuxuat] ([CuaXuatId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    CREATE INDEX [IX_Nhap_CuaNhapId] ON [Nhap] ([CuaNhapId]);
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230726093226_Initial')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230726093226_Initial', N'6.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Nhap]') AND [c].[name] = N'TenCuaNhap');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Nhap] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Nhap] DROP COLUMN [TenCuaNhap];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var1 sysname;
    SELECT @var1 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dulieuxuat]') AND [c].[name] = N'TenCuaXuat');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [dulieuxuat] DROP CONSTRAINT [' + @var1 + '];');
    ALTER TABLE [dulieuxuat] DROP COLUMN [TenCuaXuat];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var2 sysname;
    SELECT @var2 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChitietXuat]') AND [c].[name] = N'BienSo');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [ChitietXuat] DROP CONSTRAINT [' + @var2 + '];');
    ALTER TABLE [ChitietXuat] DROP COLUMN [BienSo];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var3 sysname;
    SELECT @var3 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChitietXuat]') AND [c].[name] = N'TenCua');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [ChitietXuat] DROP CONSTRAINT [' + @var3 + '];');
    ALTER TABLE [ChitietXuat] DROP COLUMN [TenCua];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var4 sysname;
    SELECT @var4 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[ChitietNhap]') AND [c].[name] = N'BienSo');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [ChitietNhap] DROP CONSTRAINT [' + @var4 + '];');
    ALTER TABLE [ChitietNhap] DROP COLUMN [BienSo];
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Nhap]') AND [c].[name] = N'PhutNhap');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Nhap] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [Nhap] ALTER COLUMN [PhutNhap] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Nhap]') AND [c].[name] = N'GioNhap');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Nhap] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [Nhap] ALTER COLUMN [GioNhap] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dulieuxuat]') AND [c].[name] = N'PhutXuat');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [dulieuxuat] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [dulieuxuat] ALTER COLUMN [PhutXuat] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[dulieuxuat]') AND [c].[name] = N'GioXuat');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [dulieuxuat] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [dulieuxuat] ALTER COLUMN [GioXuat] int NULL;
END;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728071746_ThayDoi28_7')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230728071746_ThayDoi28_7', N'6.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728074306_CapNhatChiTiet')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230728074306_CapNhatChiTiet', N'6.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728080829_updateDisplayname')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230728080829_updateDisplayname', N'6.0.20');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20230728083401_updateDisplayname2')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20230728083401_updateDisplayname2', N'6.0.20');
END;
GO

COMMIT;
GO

