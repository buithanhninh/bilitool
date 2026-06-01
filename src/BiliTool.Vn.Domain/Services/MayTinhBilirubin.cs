using BiliTool.Vn.Domain.Enums;
using BiliTool.Vn.Domain.ValueObjects;

namespace BiliTool.Vn.Domain.Services;

/// <summary>
/// Engine tính toán ngưỡng bilirubin KẾT HỢP:
///   1) AAP 2022 — Pediatrics. 2022;150(3):e2022058859
///   2) NICE CG98 — Jaundice in newborn babies under 28 days (updated Oct 2023)
///
/// NGUỒN DỮ LIỆU NGƯỠNG:
///   Tất cả bảng ngưỡng được xác minh qua PediTools AAP 2022 Calculator
///   (peditools.org/bili2022/) — công cụ tham chiếu chính thức được AAP khuyến nghị.
///
/// CẤU TRÚC DỮ LIỆU:
///   - Phân theo Tuổi thai (GA): 35, 36, 37, 38, 39, ≥40 tuần
///   - Phân theo Tuổi sau sinh (giờ): nội suy tuyến tính giữa các điểm
///   - Phân theo Yếu tố nguy cơ thần kinh: Có / Không
///   - 3 mốc ngưỡng: Chiếu đèn / Escalation of Care (ET-2) / Thay máu
/// </summary>
public class MayTinhBilirubin : IMayTinhBilirubin
{
    // ============================================================
    // ĐỊNH NGHĨA KIỂU DỮ LIỆU BẢNG NGƯỠNG
    // ============================================================
    private record BangNguong(int[] Hours, decimal[] Thresholds);

    // ============================================================
    // §A. BẢNG NGƯỠNG CHIẾU ĐÈN (mg/dL) — KHÔNG CÓ NGUY CƠ THẦN KINH
    // Nguồn: PediTools AAP 2022, xác minh tại peditools.org/bili2022/
    // (Các điểm tại giờ 12, 24, 48, 72, 96, 120, 144 — nội suy cho giờ còn lại)
    // ============================================================
    private static readonly Dictionary<int, BangNguong> ChieuDen_KhongNguyCo = new()
    {
        // GA ≥ 40 tuần — apogee: ~22.6 mg/dL tại 120h, plateau sau đó
        [40] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 10.4m,12.2m,13.3m,16.2m,17.0m,18.5m,19.8m,20.8m,21.8m,22.6m,23.0m }
        ),
        // GA 39 tuần
        [39] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  9.8m,11.5m,12.8m,15.5m,16.5m,18.0m,19.3m,20.3m,21.3m,22.1m,22.5m }
        ),
        // GA 38 tuần — baseline cho GA ≥38 trong phác đồ 2004
        [38] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  9.1m,10.7m,12.3m,14.8m,16.0m,17.5m,18.8m,19.8m,20.7m,21.6m,22.0m }
        ),
        // GA 37 tuần
        [37] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  8.1m, 9.6m,11.7m,14.1m,15.7m,17.0m,18.3m,19.3m,20.0m,21.0m,21.3m }
        ),
        // GA 36 tuần
        [36] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  7.1m, 8.5m,11.4m,13.7m,15.3m,16.5m,17.8m,18.8m,19.5m,20.5m,20.8m }
        ),
        // GA 35 tuần (giới hạn dưới của phác đồ)
        [35] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  6.1m, 7.5m,10.6m,12.6m,14.2m,15.4m,16.8m,17.8m,18.6m,19.7m,20.0m }
        ),
    };

    // ============================================================
    // §B. BẢNG NGƯỠNG CHIẾU ĐÈN (mg/dL) — CÓ NGUY CƠ THẦN KINH
    // Nguồn: PediTools AAP 2022
    // Thường thấp hơn khoảng 1.5–3.0 mg/dL so với không nguy cơ
    // ============================================================
    private static readonly Dictionary<int, BangNguong> ChieuDen_CoNguyCo = new()
    {
        // GA ≥ 40 tuần: ngưỡng bằng GA 38-39 (plateau sau 38 tuần với nguy cơ)
        [40] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  8.5m, 9.9m,10.5m,13.3m,14.0m,15.1m,16.6m,17.3m,18.2m,19.1m,19.5m }
        ),
        [39] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  7.8m, 9.2m,10.5m,12.9m,14.0m,15.1m,16.6m,17.3m,18.2m,19.1m,19.5m }
        ),
        [38] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  7.1m, 8.5m,10.5m,12.5m,14.0m,15.1m,16.6m,17.3m,18.2m,19.1m,19.5m }
        ),
        [37] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  6.1m, 7.5m, 10.0m,12.0m,13.5m,14.5m,15.8m,16.6m,17.4m,18.3m,18.8m }
        ),
        [36] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  5.2m, 6.5m, 9.5m,11.5m,13.0m,14.0m,15.3m,16.1m,16.9m,17.8m,18.3m }
        ),
        [35] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] {  4.3m, 5.6m, 8.9m,10.7m,12.2m,13.3m,14.6m,15.3m,16.1m,17.0m,17.5m }
        ),
    };

    // ============================================================
    // §C. BẢNG NGƯỠNG THAY MÁU (Exchange Transfusion — ET) — mg/dL
    // Nguồn: PediTools AAP 2022
    // GHI CHÚ: Phác đồ AAP 2022 CÓ chia mốc Nguy cơ thần kinh cho Thay máu (Figure 3)
    // 
    // LƯU Ý: Ngưỡng thay máu có TRẦN CỨNG ≤ 27.0 mg/dL cho GA ≥38 tuần (Không nguy cơ)
    // ============================================================
    private static readonly Dictionary<int, BangNguong> ThayCuuMau_KhongNguyCo = new()
    {
        [40] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 17.0m,18.5m,21.4m,22.9m,24.0m,25.0m,25.9m,26.5m,27.0m,27.0m,27.0m }
        ),
        [39] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 17.0m,18.5m,21.4m,22.9m,24.0m,25.0m,25.9m,26.5m,27.0m,27.0m,27.0m }
        ),
        [38] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 17.0m,18.5m,21.4m,22.9m,24.0m,25.0m,25.9m,26.5m,27.0m,27.0m,27.0m }
        ),
        [37] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 16.0m,17.8m,20.3m,21.8m,23.1m,24.1m,25.2m,25.8m,26.8m,27.0m,27.0m }
        ),
        [36] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 15.0m,16.8m,18.9m,20.4m,21.7m,22.8m,23.9m,24.5m,25.5m,26.0m,26.0m }
        ),
        [35] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 14.0m,15.5m,17.9m,19.4m,20.7m,21.8m,22.9m,23.5m,24.5m,25.0m,25.0m }
        ),
    };

    // ============================================================
    // §D. BẢNG NGƯỠNG THAY MÁU — CÓ NGUY CƠ THẦN KINH
    // Thấp hơn §C khoảng 3-4 mg/dL
    // ============================================================
    private static readonly Dictionary<int, BangNguong> ThayCuuMau_CoNguyCo = new()
    {
        [40] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 13.5m,15.0m,17.7m,19.2m,20.1m,21.1m,22.1m,22.8m,23.5m,23.5m,23.5m }
        ),
        [39] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 13.5m,15.0m,17.7m,19.2m,20.1m,21.1m,22.1m,22.8m,23.5m,23.5m,23.5m }
        ),
        [38] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 13.5m,15.0m,17.7m,19.2m,20.1m,21.1m,22.1m,22.8m,23.5m,23.5m,23.5m }
        ),
        [37] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 12.5m,14.0m,17.2m,18.8m,19.6m,20.6m,21.2m,21.9m,22.2m,22.5m,22.5m }
        ),
        [36] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 11.5m,13.0m,17.1m,18.5m,19.5m,20.5m,21.1m,21.8m,22.1m,22.5m,22.5m }
        ),
        [35] = new BangNguong(
            Hours:      new[] { 12,   18,   24,   36,   48,   60,   72,   84,   96,   120,  144 },
            Thresholds: new[] { 10.5m,12.0m,16.1m,17.7m,18.5m,19.5m,20.1m,20.8m,21.1m,21.5m,21.5m }
        ),
    };

    // ============================================================
    // §E. BẢNG NGƯỠNG NICE CG98 (µmol/L) — Dành cho trẻ ≥ 38 tuần
    // ============================================================
    private static readonly BangNguong NICE_ChieuDen_38w = new(
        Hours:      new[] { 0,  6,    12,   18,   24,   30,   36,   42,   48,   54,   60,   66,   72,   78,   84,   90,   96 },
        Thresholds: new[] { 100m,125m,150m,175m,200m,212m,225m,237m,250m,262m,275m,287m,300m,312m,325m,337m,350m }
    );

    private static readonly BangNguong NICE_ThayMau_38w = new(
        Hours:      new[] { 0,  6,    12,   18,   24,   30,   36,   42,   48,   54,   60,   66,   72,   78,   84,   90,   96 },
        Thresholds: new[] { 100m,150m,200m,250m,300m,350m,400m,450m,450m,450m,450m,450m,450m,450m,450m,450m,450m }
    );

    // ============================================================
    // PHƯƠNG THỨC TÍNH TOÁN CHÍNH (KẾT HỢP AAP 2022 + NICE CG98)
    // ============================================================

    /// <inheritdoc/>
    public KetQuaTinhToanBilirubin TinhToan(
        double tuoiGio,
        decimal bilirubinMgDl,
        int tuoiThaiTuan,
        YeuToNguyCoThanKinh yeuToNguyCo,
        TrangThaiChieuDen trangThaiChieuDen = TrangThaiChieuDen.KhongChieuDen,
        decimal? tocDoTangBili = null)
    {
        // --- Validation ---
        if (tuoiGio < 0 || tuoiGio > 672)
            throw new ArgumentOutOfRangeException(nameof(tuoiGio),
                "Tuổi phải từ 0 đến 672 giờ (28 ngày).");
        if (bilirubinMgDl <= 0)
            throw new ArgumentOutOfRangeException(nameof(bilirubinMgDl),
                "Giá trị bilirubin phải lớn hơn 0.");
        if (tuoiThaiTuan < 35)
            throw new ArgumentOutOfRangeException(nameof(tuoiThaiTuan),
                "Phác đồ AAP 2022 chỉ áp dụng cho trẻ ≥ 35 tuần tuổi thai.");

        int gaKey = Math.Clamp(tuoiThaiTuan, 35, 40);
        bool coNguyCo = yeuToNguyCo.CoNguyCoThanKinh;
        decimal biliUmolL = ChuyenDoiSangUmolL(bilirubinMgDl);
        var chuThich = new List<string>();

        // ========== AAP 2022 THRESHOLDS (mg/dL) ==========
        decimal nguongChieuDen_AAP = NhapNoiTuyenTinh(gaKey, tuoiGio,
            coNguyCo ? ChieuDen_CoNguyCo : ChieuDen_KhongNguyCo);
        decimal nguongTM_AAP = NhapNoiTuyenTinh(gaKey, tuoiGio,
            coNguyCo ? ThayCuuMau_CoNguyCo : ThayCuuMau_KhongNguyCo);

        // ========== NICE CG98 THRESHOLDS (µmol/L → mg/dL) ==========
        double niceGio = Math.Min(tuoiGio, 96); // NICE plateau tại 96h
        decimal niceCD_UmolL;
        decimal niceTM_UmolL;

        if (tuoiThaiTuan >= 38)
        {
            niceCD_UmolL = NhapNoiTuyenTinhDon(niceGio, NICE_ChieuDen_38w);
            niceTM_UmolL = NhapNoiTuyenTinhDon(niceGio, NICE_ThayMau_38w);
        }
        else
        {
            // Trẻ đẻ non (< 38 tuần): Tính bằng công thức toán học nội suy chính xác của NICE CG98
            decimal cdPlateau = (tuoiThaiTuan * 10m) - 100m; // Ngưỡng chiếu đèn tối đa tại 72h
            decimal tmPlateau = (tuoiThaiTuan * 10m);        // Ngưỡng thay máu tối đa tại 72h
            
            if (niceGio >= 72)
            {
                niceCD_UmolL = cdPlateau;
                niceTM_UmolL = tmPlateau;
            }
            else
            {
                // Nội suy tuyến tính từ 0h đến 72h (0h: CD=40, TM=80)
                niceCD_UmolL = 40m + (cdPlateau - 40m) * (decimal)(niceGio / 72.0);
                niceTM_UmolL = 80m + (tmPlateau - 80m) * (decimal)(niceGio / 72.0);
            }
        }

        decimal niceCD_MgDl = Math.Round(niceCD_UmolL / 17.104m, 1);
        decimal niceTM_MgDl = Math.Round(niceTM_UmolL / 17.104m, 1);

        // ========== KẾT HỢP: lấy ngưỡng THẤP HƠN (an toàn hơn) ==========
        decimal nguongChieuDen, nguongThayCuuMau, nguongEscalationChon;
        PhacDo phacDoQD;
        var lang = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();

        if (niceCD_MgDl <= nguongChieuDen_AAP)
        {
            nguongChieuDen = niceCD_MgDl;
            nguongThayCuuMau = niceTM_MgDl;
            // NICE CG98: Escalation (Intensify phototherapy) khi cách ngưỡng TM 50 µmol/L (~2.9 mg/dL)
            nguongEscalationChon = niceTM_MgDl - Math.Round(50m / 17.104m, 1);
            phacDoQD = PhacDo.NICE_CG98;
            
            if (lang == "en")
                chuThich.Add("Phototherapy threshold per NICE CG98 (lower than AAP 2022)");
            else if (lang == "fr")
                chuThich.Add("Seuil de photothérapie selon NICE CG98 (inférieur à AAP 2022)");
            else
                chuThich.Add("Ngưỡng chiếu đèn theo NICE CG98 (thấp hơn AAP 2022)");
        }
        else
        {
            nguongChieuDen = nguongChieuDen_AAP;
            nguongThayCuuMau = nguongTM_AAP;
            // AAP 2022: Escalation of Care khi cách ngưỡng TM 2.0 mg/dL
            nguongEscalationChon = nguongTM_AAP - 2.0m;
            phacDoQD = PhacDo.AAP2022;
            
            if (lang == "en")
                chuThich.Add($"Phototherapy threshold per AAP 2022 (GA {tuoiThaiTuan}w, {(coNguyCo ? "with" : "without")} neuro risk)");
            else if (lang == "fr")
                chuThich.Add($"Seuil de photothérapie selon AAP 2022 (AG {tuoiThaiTuan}s, {(coNguyCo ? "avec" : "sans")} risque NT)");
            else
                chuThich.Add($"Ngưỡng chiếu đèn theo AAP 2022 (GA {tuoiThaiTuan}w, {(coNguyCo ? "có" : "không")} nguy cơ TK)");
        }

        var mucDo = XacDinhMucDo(bilirubinMgDl, nguongChieuDen, nguongEscalationChon, nguongThayCuuMau);
        var thoiGianTaiKham = XacDinhThoiGianTaiKham(mucDo);

        // ========== NICE §1.5.1: KERNICTERUS RISK ==========
        bool nguyCoKernicterus = false;
        var lyDoKernicterus = new List<string>();
        if (tuoiThaiTuan >= 37 && biliUmolL > 340m)
        {
            nguyCoKernicterus = true;
            if (lang == "en")
                lyDoKernicterus.Add("Bilirubin > 340 µmol/L in infants ≥37 weeks (NICE 1.5.1)");
            else if (lang == "fr")
                lyDoKernicterus.Add("Bilirubine > 340 µmol/L chez les nourrissons ≥37 semaines (NICE 1.5.1)");
            else
                lyDoKernicterus.Add("Bilirubin > 340 µmol/L ở trẻ ≥37 tuần (NICE 1.5.1)");
        }
        decimal tocDoUmolH = tocDoTangBili.HasValue ? tocDoTangBili.Value * 17.104m : 0;
        if (tocDoTangBili.HasValue && tocDoUmolH > 8.5m)
        {
            nguyCoKernicterus = true;
            if (lang == "en")
                lyDoKernicterus.Add($"Rate of rise {tocDoUmolH:F1} µmol/L/h > 8.5 (NICE 1.5.1)");
            else if (lang == "fr")
                lyDoKernicterus.Add($"Taux d'augmentation {tocDoUmolH:F1} µmol/L/h > 8.5 (NICE 1.5.1)");
            else
                lyDoKernicterus.Add($"Tốc độ tăng {tocDoUmolH:F1} µmol/L/h > 8.5 (NICE 1.5.1)");
        }
        if (yeuToNguyCo.DauHieuBenhNaoBilirubinCap)
        {
            nguyCoKernicterus = true;
            if (lang == "en")
                lyDoKernicterus.Add("Signs of acute bilirubin encephalopathy (NICE 1.5.1)");
            else if (lang == "fr")
                lyDoKernicterus.Add("Signes d'encéphalopathie bilirubinique aiguë (NICE 1.5.1)");
            else
                lyDoKernicterus.Add("Dấu hiệu bệnh não bilirubin cấp (NICE 1.5.1)");
        }

        // ========== NICE §1.7: VÀNG DA KÉO DÀI ==========
        bool laVDKD = tuoiThaiTuan >= 37 ? tuoiGio > 336 : tuoiGio > 504;
        string? canhBaoVDKD = null;
        if (laVDKD)
        {
            if (lang == "en")
                canhBaoVDKD = $"Prolonged jaundice (>{(tuoiThaiTuan >= 37 ? 14 : 21)} days). Required: conjugated bili, FBC, blood group, DAT, metabolic screening (NICE 1.7.1)";
            else if (lang == "fr")
                canhBaoVDKD = $"Ictère prolongé (>{(tuoiThaiTuan >= 37 ? 14 : 21)} jours). Recommandé: bilirubine conjuguée, NFS, groupe sanguin, DAT, dépistage métabolique (NICE 1.7.1)";
            else
                canhBaoVDKD = $"Vàng da kéo dài (>{(tuoiThaiTuan >= 37 ? 14 : 21)} ngày). Cần: conjugated bili, FBC, blood group, DAT, metabolic screening (NICE 1.7.1)";
        }

        // ========== NICE §1.8.1: IVIG ==========
        bool canIVIG = yeuToNguyCo.CoBenhTanHuyetMienDich && tocDoTangBili.HasValue && tocDoUmolH > 8.5m;
        string? moTaIVIG = null;
        if (canIVIG)
        {
            if (lang == "en")
                moTaIVIG = "IVIG 500 mg/kg IV over 4 hours — Rh/ABO hemolytic disease + rise >8.5 µmol/L/h (NICE 1.8.1)";
            else if (lang == "fr")
                moTaIVIG = "IVIG 500 mg/kg IV en 4 heures — maladie hémolytique Rh/ABO + hausse >8.5 µmol/L/h (NICE 1.8.1)";
            else
                moTaIVIG = "IVIG 500 mg/kg truyền TM trong 4 giờ — Bệnh tan huyết Rh/ABO + tăng >8.5 µmol/L/h (NICE 1.8.1)";
        }

        // ========== NICE §1.4.5: DỪNG CHIẾU ĐÈN ==========
        bool dangChieu = trangThaiChieuDen == TrangThaiChieuDen.DangChieuDen
                      || trangThaiChieuDen == TrangThaiChieuDen.DangChieuDenTichCuc;
        decimal nguongDung_UmolL = niceCD_UmolL - 50m;
        bool coTheDung = dangChieu && biliUmolL < nguongDung_UmolL;
        bool canRebound = trangThaiChieuDen == TrangThaiChieuDen.DaDungChieuDen;

        if (coTheDung)
        {
            if (lang == "en")
                chuThich.Add("Phototherapy can be stopped: bili < threshold - 50 µmol/L (NICE 1.4.5)");
            else if (lang == "fr")
                chuThich.Add("La photothérapie peut être arrêtée: bili < seuil - 50 µmol/L (NICE 1.4.5)");
            else
                chuThich.Add("Có thể dừng chiếu đèn: bili < ngưỡng - 50 µmol/L (NICE 1.4.5)");
        }
        if (canRebound)
        {
            if (lang == "en")
                chuThich.Add("Check rebound bilirubin 12-18h after stopping (NICE 1.4.6)");
            else if (lang == "fr")
                chuThich.Add("Contrôle rebond de bilirubine 12-18h après l'arrêt (NICE 1.4.6)");
            else
                chuThich.Add("Kiểm tra rebound bilirubin 12-18h sau dừng (NICE 1.4.6)");
        }

        // ========== NICE §1.4.1-1.4.4: LỊCH ĐO LẶP ==========
        string? lichDoLap = null;
        int? gioDoLap = null;
        decimal kcNICE_UmolL = niceCD_UmolL - biliUmolL;

        if (dangChieu)
        {
            if (lang == "en")
                lichDoLap = "Repeat TSB 4-6h after starting phototherapy, then every 6-12h when stable (NICE 1.4.4)";
            else if (lang == "fr")
                lichDoLap = "Mesurer à nouveau 4-6h après le début, puis toutes les 6-12h après stabilisation (NICE 1.4.4)";
            else
                lichDoLap = "Đo lại 4-6h sau bắt đầu chiếu đèn, sau đó mỗi 6-12h khi ổn định (NICE 1.4.4)";
            gioDoLap = 6;
        }
        else if (tuoiGio <= 24 && mucDo >= MucDoNguyHiem.CanTheoDoiSat)
        {
            if (lang == "en")
                lichDoLap = "Jaundice visible within 24h: check TSB every 6h (NICE 1.2.11)";
            else if (lang == "fr")
                lichDoLap = "Ictère dans les 24h premières: mesurer la bilirubine toutes les 6h (NICE 1.2.11)";
            else
                lichDoLap = "Vàng da trong 24h đầu: đo serum bilirubin mỗi 6h (NICE 1.2.11)";
            gioDoLap = 6;
        }
        else if (kcNICE_UmolL > 0 && kcNICE_UmolL <= 50)
        {
            if (yeuToNguyCo.CoNguyCoLamSangNICE)
            {
                if (lang == "en")
                    lichDoLap = "Within 50 µmol/L below threshold + clinical risk factors → repeat in 18h (NICE 1.4.1)";
                else if (lang == "fr")
                    lichDoLap = "À moins de 50 µmol/L sous le seuil + facteurs de risque → mesurer dans les 18h (NICE 1.4.1)";
                else
                    lichDoLap = "Trong 50 µmol/L dưới ngưỡng + có nguy cơ LS → đo lại trong 18h (NICE 1.4.1)";
                gioDoLap = 18;
            }
            else
            {
                if (lang == "en")
                    lichDoLap = "Within 50 µmol/L below threshold → repeat in 24h (NICE 1.4.1)";
                else if (lang == "fr")
                    lichDoLap = "À moins de 50 µmol/L sous le seuil → mesurer dans les 24h (NICE 1.4.1)";
                else
                    lichDoLap = "Trong 50 µmol/L dưới ngưỡng → đo lại trong 24h (NICE 1.4.1)";
                gioDoLap = 24;
            }
        }
        else if (kcNICE_UmolL > 50)
        {
            if (lang == "en")
                lichDoLap = "Below threshold by >50 µmol/L — no routine repeat required (NICE 1.4.2)";
            else if (lang == "fr")
                lichDoLap = "Sous le seuil de >50 µmol/L — pas de mesure de contrôle de routine (NICE 1.4.2)";
            else
                lichDoLap = "Dưới ngưỡng >50 µmol/L — không cần đo lặp thường quy (NICE 1.4.2)";
            gioDoLap = null;
        }

        if (canIVIG)
        {
            if (lang == "en")
                chuThich.Add("IVIG indicated (NICE 1.8.1)");
            else if (lang == "fr")
                chuThich.Add("Indication d'IVIG (NICE 1.8.1)");
            else
                chuThich.Add("Chỉ định IVIG (NICE 1.8.1)");
        }
        if (nguyCoKernicterus)
        {
            if (lang == "en")
                chuThich.Add("⚠️ KERNICTERUS RISK (NICE 1.5.1)");
            else if (lang == "fr")
                chuThich.Add("⚠️ RISQUE D'ICTÈRE NUCLÉAIRE (NICE 1.5.1)");
            else
                chuThich.Add("⚠️ NGUY CƠ KERNICTERUS (NICE 1.5.1)");
        }
        if (lang == "en")
            chuThich.Add("Note: Clinical variation may exist among lab kits (NICE CG98, 03/2023)");
        else if (lang == "fr")
            chuThich.Add("Note: Des variations cliniques peuvent exister selon les kits de labo (NICE CG98, 03/2023)");
        else
            chuThich.Add("Lưu ý: Có sai lệch giữa các kit xét nghiệm bilirubin khác nhau (NICE CG98, 03/2023)");

        return new KetQuaTinhToanBilirubin
        {
            TuoiGio = tuoiGio, TuoiThaiTuan = tuoiThaiTuan,
            BilirubinMgDl = bilirubinMgDl, BilirubinUmolL = biliUmolL,
            CoNguyCoThanKinh = coNguyCo, ThoiGianTinhToan = DateTime.Now,
            // AAP 2022
            NguongChieuDen = Math.Round(nguongChieuDen, 1),
            NguongChieuDenTichCuc = Math.Round(nguongEscalationChon, 1),
            NguongThayCuuMau = Math.Round(nguongThayCuuMau, 1),
            KhoangCachDenNguongChieuDen = Math.Round(nguongChieuDen - bilirubinMgDl, 1),
            KhoangCachDenNguongThayCuuMau = Math.Round(nguongThayCuuMau - bilirubinMgDl, 1),
            // NICE CG98
            NguongChieuDen_NICE_UmolL = Math.Round(niceCD_UmolL, 0),
            NguongThayCuuMau_NICE_UmolL = Math.Round(niceTM_UmolL, 0),
            NguongChieuDen_NICE_MgDl = niceCD_MgDl,
            NguongThayCuuMau_NICE_MgDl = niceTM_MgDl,
            KhoangCachDenNguongChieuDen_NICE = Math.Round(niceCD_UmolL - biliUmolL, 0),
            KhoangCachDenNguongThayCuuMau_NICE = Math.Round(niceTM_UmolL - biliUmolL, 0),
            // Kết hợp
            MucDoNguyHiem = mucDo, ThoiGianTaiKham = thoiGianTaiKham,
            PhacDoQuyetDinh = phacDoQD,
            CanChieuDenNgay = mucDo >= MucDoNguyHiem.CanChieuDen,
            CanXemXetThayCuuMau = mucDo >= MucDoNguyHiem.CanXemXetThayMau,
            // NICE extras
            NguyCoKernicterus = nguyCoKernicterus, LyDoNguyCoKernicterus = lyDoKernicterus,
            LaVangDaKeoDai = laVDKD, CanhBaoVangDaKeoDai = canhBaoVDKD,
            CanIVIG = canIVIG, MoTaIVIG = moTaIVIG,
            CoTheDungChieuDen = coTheDung, CanKiemTraRebound = canRebound,
            LichDoLapNICE = lichDoLap, GioDoLapTiepTheo = gioDoLap,
            ChuThichThamChieu = chuThich,
        };
    }

    // ============================================================
    // NỘI SUY TUYẾN TÍNH
    // ============================================================

    /// <summary>
    /// Nội suy tuyến tính giữa các điểm trong bảng ngưỡng.
    /// - Nếu tuoiGio nhỏ hơn điểm đầu: trả về giá trị đầu
    /// - Nếu tuoiGio lớn hơn điểm cuối: trả về giá trị cuối (plateau)
    /// - Ngược lại: nội suy tuyến tính giữa 2 điểm liền kề
    /// </summary>
    private static decimal NhapNoiTuyenTinh(int gaKey, double tuoiGio,
        Dictionary<int, BangNguong> bang)
    {
        var (hours, thresholds) = bang[gaKey];

        if (tuoiGio <= hours[0])  return thresholds[0];
        if (tuoiGio >= hours[^1]) return thresholds[^1];

        for (int i = 0; i < hours.Length - 1; i++)
        {
            if (tuoiGio >= hours[i] && tuoiGio <= hours[i + 1])
            {
                decimal t = (decimal)((tuoiGio - hours[i]) / (hours[i + 1] - hours[i]));
                return thresholds[i] + t * (thresholds[i + 1] - thresholds[i]);
            }
        }
        return thresholds[^1];
    }

    /// <summary>
    /// Nội suy tuyến tính cho bảng NICE (không phân theo GA, chỉ 1 bảng duy nhất).
    /// </summary>
    private static decimal NhapNoiTuyenTinhDon(double tuoiGio, BangNguong bang)
    {
        var (hours, thresholds) = bang;
        if (tuoiGio <= hours[0])  return thresholds[0];
        if (tuoiGio >= hours[^1]) return thresholds[^1];
        for (int i = 0; i < hours.Length - 1; i++)
        {
            if (tuoiGio >= hours[i] && tuoiGio <= hours[i + 1])
            {
                decimal t = (decimal)((tuoiGio - hours[i]) / (hours[i + 1] - hours[i]));
                return thresholds[i] + t * (thresholds[i + 1] - thresholds[i]);
            }
        }
        return thresholds[^1];
    }

    // ============================================================
    // PHÂN LOẠI MỨC ĐỘ NGUY HIỂM (AAP 2022)
    // ============================================================

    private static Enums.MucDoNguyHiem XacDinhMucDo(
        decimal bili,
        decimal nguongChieuDen,
        decimal nguongEscalation,   // AAP: ET - 2.0 mg/dL | NICE: ET - 50µmol/L
        decimal nguongThayCuuMau)
    {
        // Ngưỡng thay máu: KHẨN CẤP
        if (bili >= nguongThayCuuMau)
            return Enums.MucDoNguyHiem.KhanCapThayCuuMau;

        // Ngưỡng Escalation of Care: Chiếu đèn tích cực + hội chẩn ngay + chuẩn bị thay máu
        // AAP 2022: ET - 2.0 mg/dL | NICE §1.4.9: ET - 50 µmol/L (~2.9 mg/dL)
        if (bili >= nguongEscalation)
            return Enums.MucDoNguyHiem.CanXemXetThayMau;

        // Ngưỡng chiếu đèn tích cực: nửa trên của khoảng [chiếu đèn, escalation]
        // NICE §1.4.9: Intensify phototherapy khi tiến gần ngưỡng ET
        decimal nguongTichCuc = nguongChieuDen + (nguongEscalation - nguongChieuDen) / 2;
        if (bili >= nguongTichCuc)
            return Enums.MucDoNguyHiem.CanChieuDenTichCuc;

        // Ngưỡng chiếu đèn: Bắt đầu chiếu đèn
        if (bili >= nguongChieuDen)
            return Enums.MucDoNguyHiem.CanChieuDen;

        // Trong vòng 2 mg/dL dưới ngưỡng: Cần theo dõi sát
        if (bili >= nguongChieuDen - 2.0m)
            return Enums.MucDoNguyHiem.CanTheoDoiSat;

        return Enums.MucDoNguyHiem.BinhThuong;
    }

    // ============================================================
    // XÁC ĐỊNH THỜI GIAN TÁI KHÁM (AAP 2022)
    // ============================================================

    private static ThoiGianTaiKham XacDinhThoiGianTaiKham(Enums.MucDoNguyHiem mucDo) => mucDo switch
    {
        Enums.MucDoNguyHiem.KhanCapThayCuuMau  => ThoiGianTaiKham.TrongVong4Gio,
        Enums.MucDoNguyHiem.CanXemXetThayMau   => ThoiGianTaiKham.TrongVong4Gio,
        Enums.MucDoNguyHiem.CanChieuDenTichCuc => ThoiGianTaiKham.TrongVong8Gio,
        Enums.MucDoNguyHiem.CanChieuDen        => ThoiGianTaiKham.TrongVong8Gio,
        Enums.MucDoNguyHiem.CanTheoDoiSat      => ThoiGianTaiKham.TrongVong12Gio,
        _                                      => ThoiGianTaiKham.TrongVong24Gio
    };

    // ============================================================
    // CHUYỂN ĐỔI ĐƠN VỊ (THEO AAP 2022)
    // mg/dL ↔ μmol/L: hệ số 17.104
    // ============================================================

    /// <inheritdoc/>
    public decimal ChuyenDoiSangMgDl(decimal giaTri, DonViDo donViDo)
        => donViDo == DonViDo.UmolL
            ? Math.Round(giaTri / 17.104m, 2)
            : giaTri;

    /// <inheritdoc/>
    public decimal ChuyenDoiSangUmolL(decimal mgDl)
        => Math.Round(mgDl * 17.104m, 0);

    // ============================================================
    // TÍNH TỐC ĐỘ THAY ĐỔI BILIRUBIN
    // ============================================================

    /// <inheritdoc/>
    public TocDoThayDoiBilirubin TinhTocDoThayDoi(IList<MauBilirubinTheoThoiGian> lichSuMau)
    {
        if (lichSuMau.Count < 2)
            return new TocDoThayDoiBilirubin
            {
                DuLieuKhongDu = true
            };

        var sorted = lichSuMau.OrderBy(m => m.ThoiGianLayMau).ToList();
        var results = new List<TocDoThayDoiKhoanCach>();

        for (int i = 1; i < sorted.Count; i++)
        {
            var prev = sorted[i - 1];
            var curr = sorted[i];
            double soGio = (curr.ThoiGianLayMau - prev.ThoiGianLayMau).TotalHours;

            if (soGio <= 0) continue;

            decimal tocDo = (curr.BilirubinMgDl - prev.BilirubinMgDl) / (decimal)soGio;
            results.Add(new TocDoThayDoiKhoanCach
            {
                TuMau             = i - 1,
                DenMau            = i,
                TocDoMgDlMoiGio   = Math.Round(tocDo, 3),
                SoGioGiuaHaiMau   = Math.Round((decimal)soGio, 1)
            });
        }

        // Ngưỡng tăng nhanh (AAP 2022 / Nelson):
        //   < 24 giờ tuổi: ≥ 0.3 mg/dL/giờ
        //   ≥ 24 giờ tuổi: ≥ 0.2 mg/dL/giờ
        bool tangNhanh = results.Any(r =>
        {
            bool trong24hDau = sorted[r.TuMau].TuoiGioKhiLayMau < 24;
            return trong24hDau
                ? r.TocDoMgDlMoiGio >= 0.3m
                : r.TocDoMgDlMoiGio >= 0.2m;
        });

        return new TocDoThayDoiBilirubin
        {
            DuLieuKhongDu       = false,
            TocDoTungKhoanCach  = results,
            TocDoTrungBinh      = results.Count > 0
                ? Math.Round(results.Average(r => r.TocDoMgDlMoiGio), 3)
                : 0,
            LaTangNhanh         = tangNhanh
        };
    }
}

// ============================================================
// HELPER CLASSES
// ============================================================
public record MauBilirubinTheoThoiGian(
    DateTime ThoiGianLayMau,
    decimal  BilirubinMgDl,
    double   TuoiGioKhiLayMau
);

public class TocDoThayDoiBilirubin
{
    public bool                        DuLieuKhongDu      { get; set; }
    public List<TocDoThayDoiKhoanCach> TocDoTungKhoanCach { get; set; } = new();
    public decimal                     TocDoTrungBinh     { get; set; }
    public bool                        LaTangNhanh        { get; set; }
}

public class TocDoThayDoiKhoanCach
{
    public int     TuMau            { get; set; }
    public int     DenMau           { get; set; }
    public decimal TocDoMgDlMoiGio  { get; set; }
    public decimal SoGioGiuaHaiMau  { get; set; }
}
