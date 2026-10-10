using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Threading.Tasks;
// Khai báo namespace chứa Data và Models của Nhóm 4
using HotelManager.Data;
using HotelManager.Models;

namespace HotelManager.Pages.QuanLyTienSanh
{
    // Bắt buộc phân quyền: Chỉ Lễ tân và Quản lý mới được truy cập phân hệ này
    [Authorize(Roles = "Lễ tân,Quản lý")]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Biến lưu trữ thông báo lỗi/thành công trên giao diện
        [TempData]
        public string? AlertMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // TODO: Viết logic LINQ thống kê lượng khách đến/đi/lưu trú trong ngày tại đây
                // Dữ liệu sẽ được đẩy ra Dashboard Tiền sảnh

                await Task.CompletedTask; // Tạm thời dùng để xử lý bất đồng bộ khi chưa có LINQ
                return Page();
            }
            catch (Exception ex)
            {
                // Bọc try...catch chống sập ứng dụng và thông báo lỗi
                AlertMessage = $"Lỗi hệ thống khi tải dữ liệu tiền sảnh: {ex.Message}";
                return Page();
            }
        }
    }
}