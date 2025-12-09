using System;
using System.Drawing;
using System.Data;
using System.Windows.Forms;

namespace RailTicketSystem
{
    public partial class AdminForm : Form
    {
        private DataGridView gridUsers;
        private Button btnApprove, btnRefresh; // 새로고침 버튼
        private ComboBox cbGrade;
        private TabControl tabControl;

        // 열차 등록 탭용 컨트롤
        private TextBox txtTrainNo;
        private ComboBox cbTrainType;
        private DateTimePicker dtpTime;
        private Button btnCreateSchedule;

        DBHelper db = new DBHelper();

        public AdminForm()
        {
            InitializeCustomUI();
            LoadUnapprovedUsers();
        }

        private void InitializeCustomUI()
        {
            this.Text = "관리자 모드 (Control Panel)";
            this.Size = new Size(700, 600);

            tabControl = new TabControl() { Dock = DockStyle.Fill };
            this.Controls.Add(tabControl);

            // 탭 1: 회원 승인 관리
            TabPage pageUser = new TabPage("회원 승인 관리");
            InitializeUserTab(pageUser);
            tabControl.TabPages.Add(pageUser);

            // 탭 2: 열차 및 시간표 관리
            TabPage pageTrain = new TabPage("열차/시간표 등록");
            InitializeTrainTab(pageTrain);
            tabControl.TabPages.Add(pageTrain);
        }

        // --- [탭 1] 회원 관리 UI ---
        private void InitializeUserTab(TabPage page)
        {
            Label lbl = new Label() { Text = "승인 대기 회원 목록", Location = new Point(20, 20), AutoSize = true, Font = new Font("맑은 고딕", 10, FontStyle.Bold) };
            page.Controls.Add(lbl);

            // 새로고침 버튼
            btnRefresh = new Button() { Text = "목록 새로고침(Refresh)", Location = new Point(450, 15), Size = new Size(180, 30), BackColor = Color.LightGreen };
            btnRefresh.Click += (s, e) => LoadUnapprovedUsers();
            page.Controls.Add(btnRefresh);

            gridUsers = new DataGridView() { Location = new Point(20, 50), Size = new Size(610, 350), ReadOnly = true, SelectionMode = DataGridViewSelectionMode.FullRowSelect, AllowUserToAddRows = false };
            page.Controls.Add(gridUsers);

            Label lblGrade = new Label() { Text = "부여할 등급:", Location = new Point(20, 420), AutoSize = true };
            page.Controls.Add(lblGrade);

            cbGrade = new ComboBox() { Location = new Point(110, 417), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };
            cbGrade.Items.AddRange(new string[] { "SILVER", "GOLD", "VIP" });
            cbGrade.SelectedIndex = 0;
            page.Controls.Add(cbGrade);

            btnApprove = new Button() { Text = "가입 승인 처리", Location = new Point(230, 415), Size = new Size(150, 30), BackColor = Color.Yellow };
            btnApprove.Click += BtnApprove_Click;
            page.Controls.Add(btnApprove);
        }

        // --- [탭 2] 열차/시간표 UI ---
        private void InitializeTrainTab(TabPage page)
        {
            Label lblTitle = new Label() { Text = "신규 열차 등록 및 1개월 스케줄 생성", Location = new Point(20, 20), AutoSize = true, Font = new Font("맑은 고딕", 12, FontStyle.Bold) };
            page.Controls.Add(lblTitle);

            // 1. 열차 번호 입력
            Label lblNo = new Label() { Text = "열차 번호 (예: KTX01):", Location = new Point(30, 70), AutoSize = true };
            txtTrainNo = new TextBox() { Location = new Point(200, 67), Width = 150 };
            page.Controls.Add(lblNo); page.Controls.Add(txtTrainNo);

            // 2. 등급 선택
            Label lblType = new Label() { Text = "열차 등급:", Location = new Point(30, 110), AutoSize = true };
            cbTrainType = new ComboBox() { Location = new Point(200, 107), Width = 150, DropDownStyle = ComboBoxStyle.DropDownList };
            cbTrainType.Items.AddRange(new string[] { "SM", "MG" }); // 새마을, 무궁화
            cbTrainType.SelectedIndex = 0;
            page.Controls.Add(lblType); page.Controls.Add(cbTrainType);

            // 3. 출발 기준 시간
            Label lblTime = new Label() { Text = "서울 출발 기준 시각:", Location = new Point(30, 150), AutoSize = true };
            dtpTime = new DateTimePicker() { Location = new Point(200, 147), Format = DateTimePickerFormat.Time, ShowUpDown = true, Width = 150 };
            page.Controls.Add(lblTime); page.Controls.Add(dtpTime);

            // 4. 생성 버튼
            btnCreateSchedule = new Button() { Text = "열차 등록 및 1개월 운행 개시", Location = new Point(30, 200), Size = new Size(320, 50), BackColor = Color.SkyBlue, Font = new Font("맑은 고딕", 10, FontStyle.Bold) };
            btnCreateSchedule.Click += BtnCreateSchedule_Click;
            page.Controls.Add(btnCreateSchedule);

            // 설명 라벨
            Label lblDesc = new Label() { Text = "※ 버튼 클릭 시 수행 작업:\n1. 열차 정보 등록\n2. 좌석 정보(호차) 자동 생성\n3. 오늘부터 30일간 상행/하행 시간표 자동 생성", Location = new Point(30, 270), AutoSize = true, ForeColor = Color.Gray };
            page.Controls.Add(lblDesc);
        }

        private void LoadUnapprovedUsers()
        {
            string sql = "SELECT 회원번호, 회원이름, 휴대전화, 등급, 승인여부 FROM 회원 WHERE 승인여부 = 'N'";
            gridUsers.DataSource = db.GetDataTable(sql);
        }


        private void BtnApprove_Click(object sender, EventArgs e) //회원 승인 및 등급 부여
        {
            if (gridUsers.SelectedRows.Count == 0) return;
            string id = gridUsers.SelectedRows[0].Cells["회원번호"].Value.ToString();
            string grade = cbGrade.SelectedItem.ToString();

            try
            {   // 등급을 정하여 '승인여부 = Y'로 DB 업데이트
                string sql = $"UPDATE 회원 SET 승인여부 = 'Y', 등급 = '{grade}' WHERE 회원번호 = '{id}'";
                db.ExecuteQuery(sql);
                MessageBox.Show($"{id}님을 승인했습니다.");
                LoadUnapprovedUsers();
            }
            catch (Exception ex) { MessageBox.Show("오류: " + ex.Message); }
        }

        // 열차 등록 및 1개월치 스케줄 생성 로직
        private void BtnCreateSchedule_Click(object sender, EventArgs e)
        {
            string tNo = txtTrainNo.Text.Trim();
            string grade = cbTrainType.SelectedItem.ToString();

            if (string.IsNullOrEmpty(tNo)) { MessageBox.Show("열차 번호를 입력하세요."); return; }

            try
            {
                // 1. 열차 등록 (이미 있으면 무시)
                string sqlTrain = $"INSERT IGNORE INTO 열차 (열차번호, 열차등급) VALUES ('{tNo}', '{grade}')";
                db.ExecuteQuery(sqlTrain);

                // 2. 좌석 자동 생성 (SM=4석, MG=6석 )
                int seatsPerCar = (grade == "SM") ? 4 : 6;
                // 기존 좌석 지우고 다시 생성 
                db.ExecuteQuery($"DELETE FROM 열차좌석 WHERE 열차번호 = '{tNo}'");

                // 1호차, 2호차 생성
                for (int car = 1; car <= 2; car++)
                {
                    char rowChar = 'A';
                    for (int s = 1; s <= seatsPerCar; s++)
                    {
                        // 1A, 1B ... 2A, 2B 로직 (간단하게 1호차 1~N번으로 A,B,C...순서)
                        // 요구사항 기준: 1A, 1B, 2A, 2B (SM) / 1A, 1B, 1C, 2A, 2B, 2C (MG)
                        // 루프:
                        // 4석일 경우: 1A, 1B, 2A, 2B 
                        // 6석일 경우: 1A, 1B, 1C, 2A, 2B, 2C

                        string seatNum = "";
                        int rowNum = (s - 1) / (seatsPerCar / 2) + 1; // 1열 or 2열
                        int colNum = (s - 1) % (seatsPerCar / 2);     // 0, 1, 2 (A, B, C)
                        char colChar = (char)('A' + colNum);
                        seatNum = $"{rowNum}{colChar}";

                        string sqlSeat = $"INSERT INTO 열차좌석 (열차번호, 차량번호, 좌석번호) VALUES ('{tNo}', '{car}', '{seatNum}')";
                        db.ExecuteQuery(sqlSeat);
                    }
                }

                // 3. 1개월(30일)치 시간표 자동 생성 (상행 1회, 하행 1회)
                DateTime startDate = DateTime.Today;
                DateTime baseTime = dtpTime.Value; // 서울 출발 시간

                for (int i = 0; i < 30; i++)// 30일 반복
                {
                    string targetDate = startDate.AddDays(i).ToString("yyyy-MM-dd");

                    // [하행] 서울(1) -> 부산(5)
                    // 역간 1시간 소요된다고 가정
                    for (int st = 1; st <= 5; st++)// 역 순번 1부터 5까지
                    {   // 시간 계산 및 SQL INSERT INTO 운행시간표
                        // 1->2->3->4->5 순서로 시간 흐름 (서울 0시간 -> 부산 4시간 뒤)
                        string time = baseTime.AddHours(st - 1).ToString("HH:mm:ss");
                        string fullTime = $"{targetDate} {time}";

                        // 중복 방지하며 insert
                        string sqlDown = $"INSERT IGNORE INTO 운행시간표 (역순번, 열차번호, 방향, 시간) VALUES ({st}, '{tNo}', '하행', '{fullTime}')";
                        db.ExecuteQuery(sqlDown);
                    }

                    // [상행] 부산(5) -> 서울(1)
                    // 하행 출발 5시간 뒤에 부산에서 출발한다고 가정
                    DateTime upBaseTime = baseTime.AddHours(5);
                    for (int st = 5; st >= 1; st--)
                    {   // 시간 계산 및 SQL INSERT INTO 운행시간표
                        // 5->4->3->2->1 순서로 시간 흐름 (부산 0시간 -> 서울 4시간 뒤)
                        int elapsed = 5 - st;
                        string time = upBaseTime.AddHours(elapsed).ToString("HH:mm:ss");
                        string fullTime = $"{targetDate} {time}";

                        // 중복 방지하며 insert
                        string sqlUp = $"INSERT IGNORE INTO 운행시간표 (역순번, 열차번호, 방향, 시간) VALUES ({st}, '{tNo}', '상행', '{fullTime}')";
                        db.ExecuteQuery(sqlUp);
                    }
                }

                MessageBox.Show($"[{tNo}] 열차 등록 및 30일간의 스케줄/좌석 생성이 완료되었습니다!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("등록 실패: " + ex.Message);
            }
        }
    }
}