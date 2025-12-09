using System;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace RailTicketSystem
{
    public partial class CheckForm : Form
    {
        private DataGridView grid;
        private Button btnCancel;
        private Button btnModify;
        DBHelper db = new DBHelper();

        public CheckForm()
        {
            InitializeCustomUI();
            LoadMyReservations();
        }

        private void InitializeCustomUI()
        {
            this.Text = "나의 예매 내역";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;

            Label lblTitle = new Label() { Text = "예매 내역 조회", Location = new Point(20, 20), AutoSize = true, Font = new Font("맑은 고딕", 12, FontStyle.Bold) };
            this.Controls.Add(lblTitle);

            // 그리드 설정
            grid = new DataGridView();
            grid.Location = new Point(20, 60);
            grid.Size = new Size(940, 400);
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            this.Controls.Add(grid);

            int btnY = 480;

            // 수정 버튼
            btnModify = new Button() { Text = "좌석 변경 (수정)", Location = new Point(300, btnY), Size = new Size(180, 50), BackColor = Color.LightYellow };
            btnModify.Click += BtnModify_Click;
            this.Controls.Add(btnModify);

            // 취소 버튼
            btnCancel = new Button() { Text = "선택 예매 취소", Location = new Point(500, btnY), Size = new Size(180, 50), BackColor = Color.LightPink };
            btnCancel.Click += BtnCancel_Click;
            this.Controls.Add(btnCancel);
        }

        private void LoadMyReservations()
        {
            try
            {
                string myID = Form1.CurrentUserID;
                //예매 내역 조회(출발 / 도착 시각 및 결제 정보 포함)
                string sql = $@"
            SELECT 
                H.예약번호, 
                H.예매일시,          
                S.열차번호, 
                H.출발역, 
                S.시간 AS 출발시각,
                H.도착역,
                (
                    SELECT T.시간 
                    FROM 운행시간표 T 
                    JOIN 기차역 G ON T.역순번 = G.역순번
                    WHERE T.열차번호 = S.열차번호 
                      AND G.역이름 = H.도착역 
                      AND DATE(T.시간) = S.운행날짜
                    LIMIT 1  -- [핵심 수정] 중복이 있어도 1개만 가져오게 강제함!
                ) AS 도착시각,
                S.차량번호, 
                S.좌석번호,          
                H.금액,              
                H.결제방법           
            FROM 예약현황 H
            JOIN 예약좌석 S ON H.예약번호 = S.예약번호
            JOIN 기차역 G_Start ON S.역순번 = G_Start.역순번
            WHERE H.회원번호 = '{myID}'
              AND G_Start.역이름 = H.출발역
            ORDER BY H.예매일시 DESC";

                grid.DataSource = db.GetDataTable(sql);
            }
            catch (Exception ex)
            {
                MessageBox.Show("목록 로드 오류: " + ex.Message);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0) return;
            if (MessageBox.Show("취소하시겠습니까?", "확인", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                try
                {
                    string resNo = grid.SelectedRows[0].Cells["예약번호"].Value.ToString();
                    string seat = grid.SelectedRows[0].Cells["좌석번호"].Value.ToString();

                    string sqlCheck = $"SELECT COUNT(*) FROM 예약좌석 WHERE 예약번호='{resNo}'";
                    int cnt = Convert.ToInt32(db.GetDataTable(sqlCheck).Rows[0][0]);

                    string sqlDelDetail = $"DELETE FROM 예약좌석 WHERE 예약번호='{resNo}' AND 좌석번호='{seat}'";
                    db.ExecuteQuery(sqlDelDetail);

                    if (cnt <= 1)
                    {
                        db.ExecuteQuery($"DELETE FROM 예약현황 WHERE 예약번호='{resNo}'");
                    }

                    MessageBox.Show("취소되었습니다.");
                    LoadMyReservations();
                }
                catch (Exception ex) { MessageBox.Show("오류: " + ex.Message); }
            }
        }

        private void BtnModify_Click(object sender, EventArgs e)
        {
            if (grid.SelectedRows.Count == 0) { MessageBox.Show("변경할 예약을 선택하세요."); return; }

            string resNo = grid.SelectedRows[0].Cells["예약번호"].Value.ToString();
            string oldSeat = grid.SelectedRows[0].Cells["좌석번호"].Value.ToString();
            string carNum = grid.SelectedRows[0].Cells["차량번호"].Value.ToString();
            string trainNo = grid.SelectedRows[0].Cells["열차번호"].Value.ToString();
            string start = grid.SelectedRows[0].Cells["출발역"].Value.ToString();
            string end = grid.SelectedRows[0].Cells["도착역"].Value.ToString();

            DateTime dtObj = Convert.ToDateTime(grid.SelectedRows[0].Cells["출발시각"].Value);
            string date = dtObj.ToString("yyyy-MM-dd");

            SeatChangeForm changeForm = new SeatChangeForm(resNo, oldSeat, trainNo, date, start, end);
            changeForm.ShowDialog();

            LoadMyReservations();
        }
    }
}