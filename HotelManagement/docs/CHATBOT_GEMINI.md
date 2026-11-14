# Chatbot AI Gemini

## Cấu hình API key

Không đặt API key trực tiếp trong `appsettings.json` hoặc mã nguồn. Dự án đã có User Secrets ID, có thể cấu hình bằng PowerShell tại thư mục chứa file `.csproj`:

```powershell
dotnet user-secrets set "Gemini:ApiKey" "YOUR_GEMINI_API_KEY"
```

Hoặc dùng biến môi trường:

```powershell
$env:Gemini__ApiKey = "YOUR_GEMINI_API_KEY"
```

Có thể thay đổi model bằng `Gemini:Model` nếu tài khoản Gemini sử dụng model khác. Các giá trị mặc định trong `appsettings.json` gồm endpoint, timeout và giới hạn token.

## Endpoint

`POST /api/chatbot/message`

```json
{
  "message": "Hôm nay còn bao nhiêu phòng trống?",
  "checkIn": "2026-12-01",
  "checkOut": "2026-12-03"
}
```

`checkIn` và `checkOut` là tùy chọn, dùng khi khách hàng hỏi availability theo khoảng ngày.

## Phân quyền và dữ liệu

- Khách chưa đăng nhập hoặc khách hàng: chatbot chỉ nhận context công khai từ `CaiDatHeThongs`, loại phòng, giá phòng và tình trạng phòng tổng hợp.
- `Admin` hoặc `NhanVien`: chatbot nhận dữ liệu vận hành đã tổng hợp từ database như trạng thái phòng, trạng thái đặt phòng, doanh thu tháng, đơn chờ xác nhận, tồn kho thấp và các đặt phòng gần đây.
- Chatbot không thực hiện thao tác ghi/xóa/cập nhật dữ liệu.
- Không đưa API key vào JavaScript; trình duyệt chỉ gọi endpoint nội bộ `/api/chatbot/message`.

## Lưu ý vận hành

Gemini có thể trả lời không chính xác nếu dữ liệu chưa được cập nhật hoặc câu hỏi vượt ngoài context. Prompt server yêu cầu AI không bịa dữ liệu; khi không có thông tin, chatbot phải báo rõ chưa có dữ liệu. Cần theo dõi quota Gemini và nên bổ sung rate limiting trước khi triển khai công khai.
