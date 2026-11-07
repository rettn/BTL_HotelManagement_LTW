using System.ComponentModel.DataAnnotations;

namespace HotelManagement.Models
{
    public class PaymentSetting
    {
        [Required(ErrorMessage = "Vui lòng chọn ngân hàng.")]
        [Display(Name = "Mã ngân hàng (VietQR)")]
        public string NganHangMa { get; set; } = "MB";

        [Display(Name = "Tên ngân hàng")]
        public string TenNganHang { get; set; } = "MBBank - Ngân hàng TMCP Quân Đội";

        [Required(ErrorMessage = "Vui lòng nhập số tài khoản ngân hàng.")]
        [Display(Name = "Số tài khoản ngân hàng")]
        public string SoTaiKhoan { get; set; } = "0379111973";

        [Required(ErrorMessage = "Vui lòng nhập tên chủ tài khoản.")]
        [Display(Name = "Tên chủ tài khoản (viết hoa không dấu)")]
        public string TenChuTaiKhoan { get; set; } = "DANG THE TOAN";

        [Display(Name = "SePay API Key / Token")]
        public string SePayApiKey { get; set; } = "";

        [Display(Name = "SePay API URL")]
        public string SePayApiUrl { get; set; } = "https://my.sepay.vn/userapi/transactions/list";

        [Display(Name = "Tiền tố nội dung chuyển khoản")]
        public string NoiDungPrefix { get; set; } = "DP";

        [Display(Name = "Tỷ lệ thanh toán trước (%)")]
        [Range(1, 100, ErrorMessage = "Tỷ lệ thanh toán từ 1% đến 100%")]
        public int TiLeThanhToanPercent { get; set; } = 100;

        // Danh sách các ngân hàng phổ biến tại Việt Nam hỗ trợ VietQR
        public static readonly List<BankInfo> DanhSachNganHang = new()
        {
            new BankInfo { Ma = "MB", Ten = "MBBank - Ngân hàng TMCP Quân Đội", Bin = "970422" },
            new BankInfo { Ma = "VCB", Ten = "Vietcombank - Ngân hàng TMCP Ngoại Thương Việt Nam", Bin = "970436" },
            new BankInfo { Ma = "ICB", Ten = "VietinBank - Ngân hàng TMCP Công Thương Việt Nam", Bin = "970415" },
            new BankInfo { Ma = "BIDV", Ten = "BIDV - Ngân hàng TMCP Đầu tư và Phát triển Việt Nam", Bin = "970418" },
            new BankInfo { Ma = "TCB", Ten = "Techcombank - Ngân hàng TMCP Kỹ Thương Việt Nam", Bin = "970407" },
            new BankInfo { Ma = "ACB", Ten = "ACB - Ngân hàng TMCP Á Châu", Bin = "970416" },
            new BankInfo { Ma = "VPB", Ten = "VPBank - Ngân hàng TMCP Việt Nam Thịnh Vượng", Bin = "970432" },
            new BankInfo { Ma = "TPB", Ten = "TPBank - Ngân hàng TMCP Tiên Phong", Bin = "970423" },
            new BankInfo { Ma = "STB", Ten = "Sacombank - Ngân hàng TMCP Sài Gòn Thương Tín", Bin = "970403" },
            new BankInfo { Ma = "HDB", Ten = "HDBank - Ngân hàng TMCP Phát triển TP.HCM", Bin = "970437" },
            new BankInfo { Ma = "VIB", Ten = "VIB - Ngân hàng TMCP Quốc Tế Việt Nam", Bin = "970441" },
            new BankInfo { Ma = "SHB", Ten = "SHB - Ngân hàng TMCP Sài Gòn - Hà Nội", Bin = "970443" },
            new BankInfo { Ma = "MSB", Ten = "MSB - Ngân hàng TMCP Hàng Hải", Bin = "970426" },
            new BankInfo { Ma = "OCB", Ten = "OCB - Ngân hàng TMCP Phương Đông", Bin = "970448" },
            new BankInfo { Ma = "VBA", Ten = "Agribank - Ngân hàng Nông nghiệp & Phát triển Nông thôn", Bin = "970405" }
        };
    }

    public class BankInfo
    {
        public string Ma { get; set; } = string.Empty;
        public string Ten { get; set; } = string.Empty;
        public string Bin { get; set; } = string.Empty;
    }
}
