using BiliTool.Vn.Application.DTOs;
using BiliTool.Vn.Application.Services;
using BiliTool.Vn.Domain.Clinical.Bilirubin;
using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Domain.Services;
using BiliTool.Vn.Domain.ValueObjects;
using MediatR;

namespace BiliTool.Vn.Application.Commands;

// ============================================================
// COMMAND
// ============================================================
/// <summary>Lệnh tính toán bilirubin (CQRS Command)</summary>
public record TinhToanBilirubinCommand(YeuCauTinhToanBilirubinDto YeuCau)
    : IRequest<KetQuaTinhToanDto>;

// ============================================================
// HANDLER
// ============================================================
public class TinhToanBilirubinHandler
    : IRequestHandler<TinhToanBilirubinCommand, KetQuaTinhToanDto>
{
    private readonly IMayTinhBilirubin _mayTinh;
    private readonly IBilirubinClinicalFacade _clinicalFacade;
    private readonly IClinicalAuditService? _clinicalAuditService;

    public TinhToanBilirubinHandler(
        IMayTinhBilirubin mayTinh,
        IBilirubinClinicalFacade clinicalFacade,
        IClinicalAuditService? clinicalAuditService = null)
    {
        _mayTinh = mayTinh;
        _clinicalFacade = clinicalFacade;
        _clinicalAuditService = clinicalAuditService;
    }

    public async Task<KetQuaTinhToanDto> Handle(
        TinhToanBilirubinCommand request,
        CancellationToken cancellationToken)
    {
        var yc = request.YeuCau;

        // Tính tuổi theo giờ
        double tuoiGio = TinhTuoiGio(yc);

        // Chuyển đổi bilirubin sang mg/dL
        decimal bilirubinMgDl = _mayTinh.ChuyenDoiSangMgDl(yc.TongBilirubin, yc.DonViDo);

        // Map yếu tố nguy cơ (AAP 2022 + NICE CG98)
        var yeuToNguyCo = new YeuToNguyCoThanKinh
        {
            BenhTanHuyetMienDichHoacThieuG6PD = yc.YeuToNguyCo.BenhTanHuyetMienDichHoacThieuG6PD,
            NhiemKhuanHuyetHoacNghiNgo = yc.YeuToNguyCo.NhiemKhuanHuyetHoacNghiNgo,
            AlbuminThapDuoi3gDl = yc.YeuToNguyCo.AlbuminThapDuoi3gDl,
            ETCOcPpm = yc.YeuToNguyCo.ETCOcPpm,
            TinhTrangLamSangKhongOnDinh = yc.YeuToNguyCo.TinhTrangLamSangKhongOnDinh,
            // NICE CG98
            AnhChiBiVangDaCanChieuDen = yc.YeuToNguyCo.AnhChiBiVangDaCanChieuDen,
            MeBuMeHoanToan = yc.YeuToNguyCo.MeBuMeHoanToan,
            VangDaTrong24hDau = yc.YeuToNguyCo.VangDaTrong24hDau,
            BenhTanHuyetRh = yc.YeuToNguyCo.BenhTanHuyetRh,
            BenhTanHuyetABO = yc.YeuToNguyCo.BenhTanHuyetABO,
            DauHieuBenhNaoBilirubinCap = yc.YeuToNguyCo.DauHieuBenhNaoBilirubinCap,
        };

        // Tính toán kết hợp AAP 2022 + NICE CG98
        var clinicalResult = _clinicalFacade.TinhToan(tuoiGio, bilirubinMgDl, yc.TuoiThaiTuan,
            yeuToNguyCo, yc.TrangThaiChieuDen);
        var ketQua = clinicalResult.KetQua;

        // Map sang DTO
        var dto = new KetQuaTinhToanDto
        {
            TuoiGio = ketQua.TuoiGio,
            TuoiThaiTuan = ketQua.TuoiThaiTuan,
            BilirubinMgDl = bilirubinMgDl,
            BilirubinUmolL = ketQua.BilirubinUmolL,
            CoNguyCoThanKinh = ketQua.CoNguyCoThanKinh,
            // AAP 2022
            NguongChieuDen = ketQua.NguongChieuDen,
            NguongChieuDenTichCuc = ketQua.NguongChieuDenTichCuc,
            NguongThayCuuMau = ketQua.NguongThayCuuMau,
            KhoangCachDenNguongChieuDen = ketQua.KhoangCachDenNguongChieuDen,
            KhoangCachDenNguongThayCuuMau = ketQua.KhoangCachDenNguongThayCuuMau,
            // NICE CG98
            NguongChieuDen_NICE_UmolL = ketQua.NguongChieuDen_NICE_UmolL,
            NguongThayCuuMau_NICE_UmolL = ketQua.NguongThayCuuMau_NICE_UmolL,
            NguongChieuDen_NICE_MgDl = ketQua.NguongChieuDen_NICE_MgDl,
            NguongThayCuuMau_NICE_MgDl = ketQua.NguongThayCuuMau_NICE_MgDl,
            KhoangCachDenNguongChieuDen_NICE = ketQua.KhoangCachDenNguongChieuDen_NICE,
            KhoangCachDenNguongThayCuuMau_NICE = ketQua.KhoangCachDenNguongThayCuuMau_NICE,
            // Kết hợp
            CanChieuDenNgay = ketQua.CanChieuDenNgay,
            CanChieuDenTichCuc = ketQua.CanChieuDenTichCuc,
            CanXemXetThayCuuMau = ketQua.CanXemXetThayCuuMau,
            MucDoNguyHiemEnum = ketQua.MucDoNguyHiem,
            MauNguyHiem = LayMauHienThi(ketQua.MucDoNguyHiem),
            ThoiGianTaiKhamEnum = ketQua.ThoiGianTaiKham,
            PhacDoQuyetDinh = ketQua.PhacDoQuyetDinh,
            // NICE extras
            NguyCoKernicterus = ketQua.NguyCoKernicterus,
            LyDoNguyCoKernicterus = ketQua.LyDoNguyCoKernicterus,
            LaVangDaKeoDai = ketQua.LaVangDaKeoDai,
            CanhBaoVangDaKeoDai = ketQua.CanhBaoVangDaKeoDai,
            CanIVIG = ketQua.CanIVIG,
            MoTaIVIG = ketQua.MoTaIVIG,
            CoTheDungChieuDen = ketQua.CoTheDungChieuDen,
            CanKiemTraRebound = ketQua.CanKiemTraRebound,
            LichDoLapNICE = ketQua.LichDoLapNICE,
            GioDoLapTiepTheo = ketQua.GioDoLapTiepTheo,
            ChuThichThamChieu = ketQua.ChuThichThamChieu,
            ThoiGianLayMau = LayThoiGianLayMau(yc),
            ThoiGianTinhToan = ketQua.ThoiGianTinhToan,
        };

        // Generate chart data for nomogram (up to 336 hours / 14 days)
        var chartData = new List<ChartDataPoint>();
        int maxChartHour = Math.Max(336, (int)Math.Ceiling(tuoiGio) + 24); 
        // Ensure the chart at least covers the current age + 24h
        if (maxChartHour > 672) maxChartHour = 672;

        for (int h = 0; h <= maxChartHour; h += 12)
        {
            try 
            {
                var pointResult = _mayTinh.TinhToan(
                    h == 0 ? 1 : h, // Tuổi >= 1
                    1.0m, // dummy bili
                    request.YeuCau.TuoiThaiTuan,
                    yeuToNguyCo,
                    TrangThaiChieuDen.KhongChieuDen,
                    null
                );
                
                chartData.Add(new ChartDataPoint 
                {
                    Hour = h,
                    Phototherapy = pointResult.NguongChieuDen,
                    Escalation = pointResult.NguongChieuDenTichCuc,
                    Exchange = pointResult.NguongThayCuuMau
                });
            }
            catch 
            {
                // Ignore if out of bounds for some reason
            }
        }
        dto.ChartData = chartData;

        if (_clinicalAuditService != null)
        {
            await _clinicalAuditService.TryRecordCalculationAsync(yc, dto, clinicalResult.Trace, cancellationToken);
        }

        return dto;
    }

    private static double TinhTuoiGio(YeuCauTinhToanBilirubinDto yc)
    {
        if (yc.TuoiTheoGio.HasValue)
            return yc.TuoiTheoGio.Value;

        if (yc.NgaySinh.HasValue && yc.NgayLayMau.HasValue)
        {
            var thoiGianSinh = yc.GioSinh.HasValue
                ? yc.NgaySinh.Value.Date + yc.GioSinh.Value
                : yc.NgaySinh.Value;

            var thoiGianLayMau = yc.GioLayMau.HasValue
                ? yc.NgayLayMau.Value.Date + yc.GioLayMau.Value
                : yc.NgayLayMau.Value;

            return (thoiGianLayMau - thoiGianSinh).TotalHours;
        }

        throw new InvalidOperationException(
            "Phải cung cấp hoặc ngày giờ sinh + ngày giờ lấy mẫu, hoặc tuổi tính theo giờ.");
    }

    private static DateTime? LayThoiGianLayMau(YeuCauTinhToanBilirubinDto yc)
    {
        if (!yc.NgayLayMau.HasValue) return null;
        return yc.GioLayMau.HasValue
            ? yc.NgayLayMau.Value.Date + yc.GioLayMau.Value
            : yc.NgayLayMau.Value;
    }

    private static string LayMauHienThi(MucDoNguyHiem mucDo) => mucDo switch
    {
        MucDoNguyHiem.BinhThuong => "#17693a",          // xanh lá đậm, đạt tương phản WCAG AA
        MucDoNguyHiem.CanTheoDoiSat => "#f39c12",       // vàng
        MucDoNguyHiem.CanChieuDen => "#e67e22",         // cam
        MucDoNguyHiem.CanChieuDenTichCuc => "#e74c3c",  // đỏ
        MucDoNguyHiem.CanXemXetThayMau => "#c0392b",    // đỏ đậm
        MucDoNguyHiem.KhanCapThayCuuMau => "#8e1010",   // đỏ rất đậm
        _ => "#7f8c8d"
    };
}
