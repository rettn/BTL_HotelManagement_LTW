Hướng dẫn tạo tài khoản admin để chạy thử vào trang quản trị
trên cửa sổ terminal của thư mục chứa file csproj chạy lần lượt cách lệnh

dotnet user-secrets init

dotnet user-secrets set "SeedAdmin:Email" "admin@hotel.local"

dotnet user-secrets set "SeedAdmin:Password" "Admin@12345"

//email và mật khẩu mình có thể tùy biến nhưng mật khẩu phải đảm bảo quy tắc an toàn, không là chương trình ko chạy 
