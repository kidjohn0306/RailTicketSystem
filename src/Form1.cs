using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;

namespace RailTicketSystem
{
    public partial class Form1 : Form
    {
        private TextBox txtID;
        private TextBox txtPW;
        private Button btnLogin;
        private Button btnRegister;
        private Label lblID;
        private Label lblPW;

        public static string CurrentUserID = "";
        DBHelper db = new DBHelper();

        public Form1()
        {
            InitializeComponent();
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            this.Text = "철도 예약 시스템";
            this.Size = new Size(350, 250);
            this.StartPosition = FormStartPosition.CenterScreen;

            lblID = new Label() { Text = "아이디 :", Location = new Point(30, 30), AutoSize = true };
            this.Controls.Add(lblID);

            txtID = new TextBox() { Location = new Point(100, 27), Size = new Size(150, 25) };
            this.Controls.Add(txtID);

            lblPW = new Label() { Text = "비밀번호 :", Location = new Point(30, 70), AutoSize = true };
            this.Controls.Add(lblPW);

            txtPW = new TextBox() { Location = new Point(100, 67), Size = new Size(150, 25), PasswordChar = '*' };
            this.Controls.Add(txtPW);

            btnLogin = new Button() { Text = "로그인", Location = new Point(30, 120), Size = new Size(100, 40) };
            btnLogin.Click += new EventHandler(btnLogin_Click);
            this.Controls.Add(btnLogin);

            btnRegister = new Button() { Text = "회원가입", Location = new Point(150, 120), Size = new Size(100, 40) };
            btnRegister.Click += new EventHandler(btnRegister_Click);
            this.Controls.Add(btnRegister);
        }

        // --- 기능(로직) 구현 부분 ---

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string id = txtID.Text.Trim();
            string pw = txtPW.Text.Trim();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
            {
                MessageBox.Show("아이디와 비밀번호를 입력하세요.");
                return;
            }

            try
            {
                // 회원 정보 조회
                string query = $"SELECT 비밀번호, Salt, 회원이름, 승인여부, 등급 FROM 회원 WHERE 회원번호 = '{id}'";
                DataTable dt = db.GetDataTable(query);

                if (dt.Rows.Count == 0)
                {
                    MessageBox.Show("존재하지 않는 아이디입니다.");
                    return;
                }

                string dbPw = dt.Rows[0]["비밀번호"].ToString();
                string salt = dt.Rows[0]["Salt"].ToString();
                string name = dt.Rows[0]["회원이름"].ToString();
                string approved = dt.Rows[0]["승인여부"].ToString();
                string grade = dt.Rows[0]["등급"].ToString();

                // 비밀번호 검증
                if (Security.HashPassword(pw, salt) == dbPw)
                {
                    //admin123 계정일 때만 관리자 모드 진입
                    if (id == "admin123")
                    {
                        MessageBox.Show($"관리자({name}) 모드로 접속합니다.");
                        AdminForm admin = new AdminForm();
                        admin.Show();
                        this.Hide();
                    }
                    // 일반 회원은 승인 여부 체크
                    else if (approved == "Y")
                    {
                        MessageBox.Show($"{name}님 안녕하십니까! [{grade}]");
                        CurrentUserID = id;

                        MainForm main = new MainForm();
                        main.Show();
                        this.Hide();
                    }
                    else
                    {
                        MessageBox.Show("관리자의 승인 대기 중입니다.\n승인 후 로그인해주세요.");
                    }
                }
                else
                {
                    MessageBox.Show("비밀번호가 틀렸습니다.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("로그인 오류: " + ex.Message);
            }
        }

        private void btnRegister_Click(object sender, EventArgs e)
        {
            RegisterForm regForm = new RegisterForm();
            regForm.ShowDialog();
        }
    }
}