using System;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace RailTicketSystem
{
    public partial class ReservationForm : Form
    {
        private ComboBox cbStart, cbEnd, cbTrainType;
        private DateTimePicker dtpDate;
        private Button btnSearch;
        private DataGridView grid;

        DBHelper db = new DBHelper();

        public ReservationForm()
        {
            InitializeCustomUI();
            LoadStations();
        }

        private void InitializeCustomUI()
        {
            this.Text = "승차권 예매 (전구간 조회 가능)";
            this.Size = new Size(750, 500);
            this.StartPosition = FormStartPosition.CenterParent;

            // 1. 출발역
            Label lblStart = new Label() { Text = "출발역", Location = new Point(20, 20), AutoSize = true };
            cbStart = new ComboBox() { Location = new Point(20, 45), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };

            // 2. 도착역
            Label lblEnd = new Label() { Text = "도착역", Location = new Point(130, 20), AutoSize = true };
            cbEnd = new ComboBox() { Location = new Point(130, 45), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };

            // 3. 열차 종류
            Label lblType = new Label() { Text = "열차종류", Location = new Point(240, 20), AutoSize = true };
            cbTrainType = new ComboBox() { Location = new Point(240, 45), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cbTrainType.Items.AddRange(new string[] { "전체", "새마을(SM)", "무궁화(MG)" });
            cbTrainType.SelectedIndex = 0;

            // 4. 날짜
            Label lblDate = new Label() { Text = "출발일", Location = new Point(350, 20), AutoSize = true };
            dtpDate = new DateTimePicker() { Location = new Point(350, 45), Width = 150, Format = DateTimePickerFormat.Short };

            // 5. 조회 버튼
            btnSearch = new Button() { Text = "조회하기", Location = new Point(520, 43), Width = 100, Height = 25, BackColor = Color.LightSkyBlue };
            btnSearch.Click += BtnSearch_Click;

            this.Controls.Add(lblStart); this.Controls.Add(cbStart);
            this.Controls.Add(lblEnd); this.Controls.Add(cbEnd);
            this.Controls.Add(lblType); this.Controls.Add(cbTrainType);
            this.Controls.Add(dtpDate); this.Controls.Add(btnSearch);

            // 6. 결과 그리드
            grid = new DataGridView();
            grid.Location = new Point(20, 90);
            grid.Size = new Size(690, 350);
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.CellDoubleClick += Grid_CellDoubleClick;
            this.Controls.Add(grid);
        }

        private void LoadStations()
        {
            string[] stations = { "서울", "천안", "대전", "대구", "부산" };
            cbStart.Items.AddRange(stations);
            cbEnd.Items.AddRange(stations);
            cbStart.SelectedIndex = 0; // 서울
            cbEnd.SelectedIndex = 4;   // 부산
        }

        private void BtnSearch_Click(object sender, EventArgs e)
        {
            string start = cbStart.SelectedItem.ToString();
            string end = cbEnd.SelectedItem.ToString();
            string date = dtpDate.Value.ToString("yyyy-MM-dd");

            if (start == end) { MessageBox.Show("출발역과 도착역이 같습니다."); return; }

            // 상행/하행 판단
            int idxStart = cbStart.SelectedIndex;
            int idxEnd = cbEnd.SelectedIndex;
            string direction = (idxStart < idxEnd) ? "하행" : "상행";

            // 열차 종류 필터
            string typeCondition = "";
            if (cbTrainType.SelectedIndex == 1) typeCondition = "AND R.열차등급 = 'SM'";
            if (cbTrainType.SelectedIndex == 2) typeCondition = "AND R.열차등급 = 'MG'";

            try
            {
                // DB 조회 (중간 정차역 포함)
                string sql = $@"
                    SELECT 
                        T.열차번호, 
                        R.열차등급, 
                        T.시간 AS 출발시간, 
                        '{direction}' AS 방향
                    FROM 운행시간표 T
                    JOIN 열차 R ON T.열차번호 = R.열차번호
                    JOIN 기차역 S ON T.역순번 = S.역순번
                    WHERE S.역이름 = '{start}'
                      AND T.방향 = '{direction}'
                      AND DATE(T.시간) = '{date}'
                      {typeCondition}
                    ORDER BY T.시간 ASC";

                DataTable dt = db.GetDataTable(sql);

                // 컬럼 추가
                dt.Columns.Add("운임", typeof(string));
                dt.Columns.Add("잔여석", typeof(string));

                foreach (DataRow row in dt.Rows)
                {
                    string tNo = row["열차번호"].ToString();
                    string grade = row["열차등급"].ToString();

                    // 1. 운임 조회 (양방향 체크)
                    int price = GetPrice(start, end, grade);
                    row["운임"] = $"{price:N0}원";

                    // 2. 잔여석 계산
                    int totalSeats = (grade == "SM") ? 8 : 12; // SM: 2량x4석, MG: 2량x6석
                    int bookedSeats = GetBookedCount(tNo, date, start, end);
                    int remain = totalSeats - bookedSeats;
                    row["잔여석"] = (remain <= 0) ? "매진" : $"{remain} / {totalSeats} 석";
                }

                grid.DataSource = dt;
                if (dt.Rows.Count == 0) MessageBox.Show("배정된 열차가 없습니다.");
            }
            catch (Exception ex) { MessageBox.Show("조회 오류: " + ex.Message); }
        }

        //요금표 양방향 조회
        private int GetPrice(string start, string end, string grade)
        {
            try
            {
                string sql = $"SELECT 요금 FROM 요금표 WHERE 출발역='{start}' AND 도착역='{end}' AND 열차등급='{grade}'";
                DataTable dt = db.GetDataTable(sql);

                if (dt.Rows.Count == 0) // 반대 방향 검색
                {
                    sql = $"SELECT 요금 FROM 요금표 WHERE 출발역='{end}' AND 도착역='{start}' AND 열차등급='{grade}'";
                    dt = db.GetDataTable(sql);
                }

                if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0]["요금"]);
                return 0;
            }
            catch { return 0; }
        }

        private int GetBookedCount(string tNo, string date, string start, string end)
        {
            try
            {
                string sql = $@"
                    SELECT COUNT(DISTINCT 좌석번호) 
                    FROM 예약좌석
                    WHERE 열차번호 = '{tNo}'
                      AND 운행날짜 = '{date}'
                      AND 역순번 >= (SELECT 역순번 FROM 기차역 WHERE 역이름 = '{start}')
                      AND 역순번 <  (SELECT 역순번 FROM 기차역 WHERE 역이름 = '{end}')";

                DataTable dt = db.GetDataTable(sql);
                if (dt.Rows.Count > 0) return Convert.ToInt32(dt.Rows[0][0]);
                return 0;
            }
            catch { return 0; }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // 1. 현재 선택된 출발/도착역 가져오기
            string start = cbStart.SelectedItem.ToString();
            string end = cbEnd.SelectedItem.ToString();

            // 출발역과 도착역이 같은지 검사
            if (start == end)
            {
                MessageBox.Show("출발역과 도착역이 같습니다.\n올바른 구간을 선택한 뒤 다시 시도해주세요.");
                return; // 진행 멈춤
            }

            // 2. 매진 여부 확인
            string seatStatus = grid.Rows[e.RowIndex].Cells["잔여석"].Value.ToString();
            if (seatStatus == "매진") { MessageBox.Show("매진된 열차입니다."); return; }

            // 3. 데이터 추출 및 이동
            string tNo = grid.Rows[e.RowIndex].Cells["열차번호"].Value.ToString();
            string grd = grid.Rows[e.RowIndex].Cells["열차등급"].Value.ToString();
            string tm = Convert.ToDateTime(grid.Rows[e.RowIndex].Cells["출발시간"].Value).ToString("yyyy-MM-dd HH:mm:ss");
            string dt = dtpDate.Value.ToString("yyyy-MM-dd");

            // 좌석 선택 폼 열기
            SeatForm seatForm = new SeatForm(tNo, grd, dt, start, end, tm);
            seatForm.ShowDialog();
        }
    }
}