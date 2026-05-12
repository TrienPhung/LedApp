using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LedApp.Helpers
{
    // Provider tự động tạo Policy khi gặp [Authorize(Policy = "xxx")]
    // Không cần đăng ký từng Policy một
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallback;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
            => _fallback = new DefaultAuthorizationPolicyProvider(options);

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
            => _fallback.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
            => _fallback.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Trả về fallback cho các policy built-in
            // Policy permission thường có dạng "Module.Action"
            if (!policyName.Contains('.'))
                return _fallback.GetPolicyAsync(policyName);

            var policy = new AuthorizationPolicyBuilder()
                .AddRequirements(new PermissionRequirement(policyName))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }
    }
}