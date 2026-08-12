namespace CafePos.DTOs.Auth
{
    public class AuthResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public UserInfoDto User { get; set; } = new();
    }
    public class UserInfoDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public EmployeeInfoDto? Employee { get; set; }
    }
    public class EmployeeInfoDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int? PositionId { get; set; }
        public string PositionName { get; set; } = string.Empty;
    }
}
