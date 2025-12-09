using System;
using System.Drawing;
using System.Data;
using System.Windows.Forms;
using System.Collections.Generic;

namespace RailTicketSystem
{
    public partial class SeatChangeForm : Form
    {
        // CheckForm에서 넘겨받을 정보들
        private string resNo;      // 예약번호
        private string oldSeat;    // 원래 좌석
        private string trainNo;    // 열차번호
        private string date;       // 운행날짜
        private string startStation, endStation; // 구간

        private ComboBox cbCar;
        private FlowLayoutPanel pnlSeats;
        private Button btnConfirm;
        private string selectedSeat = ""; // 새로 선택한 좌석

        DBHelper db = new DBHelper();

        public SeatChangeForm(string resNo, string oldSeat, string tNo, string dt, string start, string end)
        {
            this.resNo = resNo;
            this.oldSeat = oldSeat;
            this.trainNo = tNo;
            this.date = dt;
            this.startStation = start;
            this.endStation = end;

            InitializeCustomUI();
            LoadCarNumbers();
        }

        private void InitializeCustomUI()
        {
            this.Text = "좌석 변경";
            this.Size = new Size(450, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            // 상단 안내
            Label lblInfo = new Label() { Text = $"현재 좌석: {oldSeat} → 변경할 좌석을 선택하세요.", Location = new Point(20, 15), AutoSize = true, Font = new Font("맑은 고딕", 10, FontStyle.Bold) };
            this.Controls.Add(lblInfo);

            // 호차 선택
            Label lblCar = new Label() { Text = "호차 선택:", Location = new Point(20, 50), AutoSize = true };
            cbCar = new ComboBox() { Location = new Point(90, 47), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cbCar.SelectedIndexChanged += (s, e) => LoadSeats();
            this.Controls.Add(lblCar); this.Controls.Add(cbCar);

            // 좌석 패널
            pnlSeats = new FlowLayoutPanel() { Location = new Point(20, 80), Size = new Size(390, 300), AutoScroll = true, BorderStyle = BorderStyle.FixedSingle };
            this.Controls.Add(pnlSeats);

            // 변경 확정 버튼
            btnConfirm = new Button() { Text = "변경 확정", Location = new Point(20, 400), Size = new Size(390, 50), BackColor = Color.LightGray, Enabled = false };
            btnConfirm.Click += BtnConfirm_Click;
            this.Controls.Add(btnConfirm);
        }

        private void LoadCarNumbers()
        {
            try
            {
                string sql = $"SELECT DISTINCT 차량번호 FROM 열차좌석 WHERE 열차번호 = '{trainNo}' ORDER BY 차량번호";
                DataTable dt = db.GetDataTable(sql);
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
                // 1. 전체 좌석 가져오기
                string sqlAll = $"SELECT 좌석번호 FROM 열차좌석 WHERE 열차번호='{trainNo}' AND 차량번호='{currentCar}' ORDER BY 좌석번호";
                DataTable dtAll = db.GetDataTable(sqlAll);

                // 2. 예약된 좌석 가져오기
                string sqlOccupied = $@"
                    SELECT DISTINCT 좌석번호 FROM 예약좌석 
                    WHERE 열차번호='{trainNo}' AND 운행날짜='{date}' AND 차량번호='{currentCar}'
                      AND 역순번 >= (SELECT 역순번 FROM 기차역 WHERE 역이름 = '{startStation}')
                      AND 역순번 <  (SELECT 역순번 FROM 기차역 WHERE 역이름 = '{endStation}')";

                DataTable dtOcc = db.GetDataTable(sqlOccupied);
                List<string> occupied = new List<string>();
                foreach (DataRow dr in dtOcc.Rows) occupied.Add(dr["좌석번호"].ToString());

                // 3. 버튼 그리기
                foreach (DataRow dr in dtAll.Rows)
                {
                    string seatNum = dr["좌석번호"].ToString();
                    Button btn = new Button() { Text = seatNum, Size = new Size(50, 40), Margin = new Padding(3) };

                    // 내 원래 자리는 '현재 좌석'이라고 표시 (초록색)
                    if (seatNum == oldSeat)
                    {
                        btn.BackColor = Color.LightGreen;
                        btn.Text = "MY";
                        btn.Enabled = false; // 내 자리는 클릭 X
                    }
                    else if (occupied.Contains(seatNum))
                    {
                        btn.BackColor = Color.Red; // 남의 자리
                        btn.Enabled = false;
                    }
                    else
                    {
                        btn.BackColor = Color.LightGray; // 빈 자리
                        btn.Click += Seat_Click;
                    }

                    pnlSeats.Controls.Add(btn);
                }
            }
            catch { }
        }

        // 좌석 클릭 (하나만 선택 가능)
        private void Seat_Click(object sender, EventArgs e)
        {
            Button clickedBtn = (Button)sender;

            // 기존 선택 취소 (파랑 -> 회색)
            foreach (Control c in pnlSeats.Controls)
            {
                if (c is Button b && b.BackColor == Color.SkyBlue)
                    b.BackColor = Color.LightGray;
            }

            // 새 좌석 선택
            clickedBtn.BackColor = Color.SkyBlue;
            selectedSeat = clickedBtn.Text;

            btnConfirm.Text = $"{selectedSeat}번 좌석으로 변경";
            btnConfirm.BackColor = Color.Yellow;
            btnConfirm.Enabled = true;
        }

        // UPDATE 쿼리
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedSeat)) return;

            try
            {
                // DB 업데이트 (예약번호와 기존 좌석번호가 일치하는 행을 찾아서 새 좌석으로 변경)
                string sql = $@"
                    UPDATE 예약좌석 
                    SET 좌석번호 = '{selectedSeat}', 
                        차량번호 = '{cbCar.SelectedItem}' 
                    WHERE 예약번호 = '{resNo}' AND 좌석번호 = '{oldSeat}'";

                db.ExecuteQuery(sql);

                MessageBox.Show($"좌석이 {oldSeat} -> {selectedSeat} 로 변경되었습니다!");
                this.Close(); // 창 닫기
            }
            catch (Exception ex)
            {
                MessageBox.Show("변경 오류: " + ex.Message);
            }
        }
    }
}