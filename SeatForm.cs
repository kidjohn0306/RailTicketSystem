using System;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RailTicketSystem
{
    public partial class SeatForm : Form
    {
        // 데이터 변수
        private string trainNo, grade, date, startStation, endStation, startTime;
        private int unitPrice = 0;
        private string userGrade;
        private double discountRate = 1.0;

        // UI 변수
        private ComboBox cbCar;
        private FlowLayoutPanel pnlSeats;
        private Label lblTotalPrice;
        private RadioButton rbCard, rbCash;
        private Button btnReserve;
        private List<string> selectedSeats = new List<string>();

        DBHelper db = new DBHelper();

        public SeatForm(string tNo, string grd, string dt, string start, string end, string time)
        {
            this.trainNo = tNo;
            this.grade = grd;
            this.date = dt;
            this.startStation = start;
            this.endStation = end;
            this.startTime = time;

            InitializeCustomUI();
            LoadUserGrade();
            GetUnitPrice();
            LoadCarNumbers();
        }

        private void InitializeCustomUI()
        {
            this.Text = "좌석 선택 및 결제";
            this.Size = new Size(550, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblInfo = new Label() { Text = $"[{trainNo}] {startStation} -> {endStation}", Location = new Point(20, 15), AutoSize = true, Font = new Font("맑은 고딕", 11, FontStyle.Bold) };
            this.Controls.Add(lblInfo);

            Label lblCar = new Label() { Text = "호차 선택:", Location = new Point(20, 50), AutoSize = true };
            cbCar = new ComboBox() { Location = new Point(90, 47), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCar.SelectedIndexChanged += (s, e) => LoadSeats();
            this.Controls.Add(lblCar); this.Controls.Add(cbCar);


            pnlSeats = new FlowLayoutPanel() { Location = new Point(20, 80), Size = new Size(500, 250), AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(pnlSeats);


            GroupBox gbPay = new GroupBox() { Text = "결제 정보", Location = new Point(20, 340), Size = new Size(500, 100) };
            lblTotalPrice = new Label() { Text = "총 결제금액: 0원", Location = new Point(20, 30), AutoSize = true, Font = new Font("맑은 고딕", 10, FontStyle.Bold), ForeColor = Color.Blue };
            gbPay.Controls.Add(lblTotalPrice);
            rbCard = new RadioButton() { Text = "신용카드", Location = new Point(20, 60), Checked = true, AutoSize = true };
            rbCash = new RadioButton() { Text = "현금", Location = new Point(100, 60), AutoSize = true };
            gbPay.Controls.Add(rbCard); gbPay.Controls.Add(rbCash);
            this.Controls.Add(gbPay);


            btnReserve = new Button() { Text = "예매 확정", Location = new Point(20, 460), Size = new Size(500, 50), BackColor = Color.LightGray, Enabled = false };
            btnReserve.Click += BtnReserve_Click;
            this.Controls.Add(btnReserve);
        }

        private void LoadUserGrade()
        {
            string myID = Form1.CurrentUserID;
            userGrade = "SILVER";

            if (string.IsNullOrEmpty(myID)) return;

            string sql = $"SELECT 등급 FROM 회원 WHERE 회원번호 = '{myID}'";
            DataTable dt = db.GetDataTable(sql);

            if (dt.Rows.Count > 0)
            {
                userGrade = dt.Rows[0]["등급"].ToString();

                switch (userGrade)
                {
                    case "VIP": discountRate = 0.90; break;
                    case "GOLD": discountRate = 0.95; break;
                    case "Manager":
                    case "SILVER":
                    default: discountRate = 1.00; break;
                }
            }
        }

        private void GetUnitPrice()
        {
            try
            {
                string sql = $"SELECT 요금 FROM 요금표 WHERE 출발역='{startStation}' AND 도착역='{endStation}' AND 열차등급='{grade}'";
                DataTable dt = db.GetDataTable(sql);

                if (dt.Rows.Count == 0)
                {
                    sql = $"SELECT 요금 FROM 요금표 WHERE 출발역='{endStation}' AND 도착역='{startStation}' AND 열차등급='{grade}'";
                    dt = db.GetDataTable(sql);
                }

                if (dt.Rows.Count > 0)
                    unitPrice = Convert.ToInt32(dt.Rows[0]["요금"]);
                else
                    unitPrice = 5000;
            }
            catch { unitPrice = 5000; }
        }

        private void LoadCarNumbers()
        {
            try
            {
                string sql = $"SELECT DISTINCT 차량번호 FROM 열차좌석 WHERE 열차번호 = '{trainNo}' ORDER BY 차량번호";
                DataTable dt = db.GetDataTable(sql);
                cbCar.Items.Clear();
                foreach (DataRow dr in dt.Rows) cbCar.Items.Add(dr["차량번호"].ToString());
                if (cbCar.Items.Count > 0) cbCar.SelectedIndex = 0;
            }
            catch { }
        }

        private void LoadSeats()
        {
            pnlSeats.Controls.Clear();
            if (cbCar.SelectedItem == null) return;
            string currentCar = cbCar.SelectedItem.ToString();

            try
            {
                string sqlAll = $"SELECT 좌석번호 FROM 열차좌석 WHERE 열차번호='{trainNo}' AND 차량번호='{currentCar}' ORDER BY 좌석번호";
                DataTable dtAll = db.GetDataTable(sqlAll);

                string sqlOccupied = $@"
                    SELECT DISTINCT 좌석번호 FROM 예약좌석 
                    WHERE 열차번호='{trainNo}' AND 운행날짜='{date}' AND 차량번호='{currentCar}'
                      AND 역순번 >= (SELECT 역순번 FROM 기차역 WHERE 역이름 = '{startStation}')
                      AND 역순번 <  (SELECT 역순번 FROM 기차역 WHERE 역이름 = '{endStation}')";

                DataTable dtOcc = db.GetDataTable(sqlOccupied);
                List<string> occupied = new List<string>();
                foreach (DataRow dr in dtOcc.Rows) occupied.Add(dr["좌석번호"].ToString());

                foreach (DataRow dr in dtAll.Rows)
                {
                    string seatNum = dr["좌석번호"].ToString();
                    Button btn = new Button() { Text = seatNum, Size = new Size(50, 40), Margin = new Padding(3) };

                    if (occupied.Contains(seatNum)) { btn.BackColor = Color.Red; btn.Enabled = false; }
                    else if (selectedSeats.Contains(seatNum)) { btn.BackColor = Color.SkyBlue; }
                    else { btn.BackColor = Color.LightGray; }

                    btn.Click += Seat_Click;
                    pnlSeats.Controls.Add(btn);
                }
            }
            catch { }
        }

        private void Seat_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string seat = btn.Text;
            if (selectedSeats.Contains(seat)) { selectedSeats.Remove(seat); btn.BackColor = Color.LightGray; }
            else { selectedSeats.Add(seat); btn.BackColor = Color.SkyBlue; }
            UpdatePriceUI();
        }

        private void UpdatePriceUI()
        {
            int count = selectedSeats.Count;
            double baseTotal = count * unitPrice;

            // 할인 적용
            int finalTotal = (int)Math.Round(baseTotal * discountRate);
            double appliedDiscount = (1 - discountRate) * 100;

            lblTotalPrice.Text = $"선택 좌석: {count}개 | 등급: {userGrade} ({appliedDiscount:N0}% 할인) | 최종 결제금액: {finalTotal:N0}원";

            if (count > 0)
            {
                btnReserve.BackColor = Color.Yellow;
                btnReserve.Enabled = true;
                btnReserve.Text = "결제 및 예매하기";
            }
            else
            {
                btnReserve.BackColor = Color.LightGray;
                btnReserve.Enabled = false;
            }
        }

        private void BtnReserve_Click(object sender, EventArgs e)
        {
            string myID = Form1.CurrentUserID;
            if (string.IsNullOrEmpty(myID)) return;

            string carNum = cbCar.SelectedItem.ToString();
            string payMethod = rbCard.Checked ? "카드" : "현금";
            int totalAmt = (int)Math.Round(selectedSeats.Count * unitPrice * discountRate); // 최종 할인 금액 재계산

            // 예매번호 생성 열차등급 + 연월일 + 순서번호 (시간 기반)
            string todayStr = DateTime.Now.ToString("yyyyMMdd");
            string timeStr = DateTime.Now.ToString("HHmmss");
            string resNo = $"{grade}{todayStr}{timeStr}";

            // 카드번호 조회
            string userCardNum = "NULL";
            if (payMethod == "카드")
            {
                try
                {
                    string sqlCard = $"SELECT 카드번호 FROM 회원 WHERE 회원번호 = '{myID}'";
                    DataTable dtC = db.GetDataTable(sqlCard);
                    if (dtC.Rows.Count > 0) userCardNum = dtC.Rows[0]["카드번호"].ToString();
                }
                catch { }
            }

            try
            {
                // 선택된 좌석 수만큼 SP 호출 (좌석 하나당 예약 구간 전체 저장)
                foreach (string seat in selectedSeats)
                {
                    // MySQL 프로시저 호출 구문
                    // @p_cardNum은 프로시저 내부에서 NULL 처리 로직이 있으므로, string "NULL" 그대로 넘김.
                    string callSP = $@"
                CALL sp_book_single_seat(
                    '{resNo}', '{myID}', '{startStation}', '{endStation}', 
                    {totalAmt}, '{payMethod}', '{userCardNum}', 
                    '{trainNo}', '{date}', '{carNum}', '{seat}', '{startTime}'
                );";

                    db.ExecuteQuery(callSP); // SP 실행
                }

                MessageBox.Show($"예매 완료! 예약번호: {resNo}\n할인 적용된 최종 금액: {totalAmt:N0}원");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("예매 실패: " + ex.Message);
            }
        }
    }
}