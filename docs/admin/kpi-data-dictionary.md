# Admin KPI Data Dictionary

## Quy ước chung
- Múi giờ lưu trữ và đối soát: UTC. UI có thể hiển thị múi giờ người dùng nhưng phải ghi nhãn.
- Dữ liệu thiếu ngưỡng AAP không được đưa vào mẫu số phân tầng; hiển thị riêng “Không đủ dữ liệu”.
- Dữ liệu test/automation phải được loại khỏi production aggregate sau khi `DataClassification` được triển khai.
- “Nguy cơ cao” không được suy ra từ bilirubin tuyệt đối; phải dùng khoảng cách tới ngưỡng theo tuổi giờ, tuổi thai và yếu tố nguy cơ.

| KPI | Nguồn | Định nghĩa |
|---|---|---|
| Tổng bác sĩ | `ho_so_nguoi_dung` | Số tài khoản người dùng hợp lệ, chưa loại test cho tới Task 6 |
| Bác sĩ hoạt động 7/30 ngày | `phien_lam_viec`, `lich_su_tinh_toan` | Bác sĩ distinct có calculation gắn `NguoiDungId` trong cửa sổ |
| Lượt tính AAP | `lich_su_tinh_toan` | Số calculation được lưu trong cửa sổ |
| Anonymous/API calculation | `phien_lam_viec`, `clinical_audit_logs` | Calculation không có user hoặc có `ApiClientId` |
| Tổng bệnh nhi | `ho_so_benh_nhan` | Số hồ sơ hợp lệ |
| Bao phủ theo dõi | `xet_nghiem_bilirubin` | Bệnh nhi có >=2 lần đo / tổng bệnh nhi |
| Bình thường | `xet_nghiem_bilirubin` | Bilirubin < ngưỡng chiếu đèn |
| Chiếu đèn | `xet_nghiem_bilirubin` | Ngưỡng chiếu đèn <= bilirubin < ngưỡng thay máu - 2 mg/dL |
| Escalation of care | `xet_nghiem_bilirubin` | Ngưỡng thay máu - 2 mg/dL <= bilirubin < ngưỡng thay máu |
| Thay máu | `xet_nghiem_bilirubin` | Bilirubin >= ngưỡng thay máu |
| Không đủ dữ liệu | `xet_nghiem_bilirubin` | Thiếu ngưỡng hoặc ngưỡng không hợp lệ |
| Lost follow-up | `xet_nghiem_bilirubin` | Hồ sơ chỉ có một lần đo; đây là tín hiệu dữ liệu, không tự động kết luận mất theo dõi lâm sàng |
| Bệnh nhi critical | `ho_so_benh_nhan` | GA <35 kèm nguy cơ thần kinh; không áp dụng classifier AAP >=35 tuần |
| Hồ sơ bác sĩ đầy đủ | `ho_so_nguoi_dung` | Có số điện thoại và đơn vị công tác |

## Gate bắt buộc
Mọi thay đổi KPI phải cập nhật tài liệu này, test aggregate và mô tả UI trong cùng thay đổi.
