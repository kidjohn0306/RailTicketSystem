using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;

namespace RailTicketSystem
{
    public partial class RegisterForm : Form
    {
        // 1. 도구 변수 선언
        private TextBox txtNewID, txtNewPW, txtName, txtPhone, txtCard;
        private Label lblID, lblPW, lblName, lblPhone, lblCard;
        private Button btnSubmit;

        DBHelper db = new DBHelper(); // DB 연결 도구

        public RegisterForm()
        {
            InitializeCustomUI();
        }

        // 2. 화면 그리기
        private void InitializeCustomUI()
        {
            this.Text = "회원가입";
            this.Size = new Size(300, 400);
            this.StartPosition = FormStartPosition.CenterParent;

            int xLabel = 30;
            int xText = 100;
            int yStart = 30;
            int step = 40;

            lblID = new Label() { Text = "아이디", Location = new Point(xLabel, yStart), AutoSize = true };
            txtNewID = new TextBox() { Location = new Point(xText, yStart - 3), Size = new Size(150, 25) };
            this.Controls.Add(lblID); this.Controls.Add(txtNewID);

            lblPW = new Label() { Text = "비밀번호", Location = new Point(xLabel, yStart + step), AutoSize = true };
            txtNewPW = new TextBox() { Location = new Point(xText, yStart + step - 3), Size = new Size(150, 25), PasswordChar = '*' };
            this.Controls.Add(lblPW); this.Controls.Add(txtNewPW);

            lblName = new Label() { Text = "이름", Location = new Point(xLabel, yStart + step * 2), AutoSize = true };
            txtName = new TextBox() { Location = new Point(xText, yStart + step * 2 - 3), Size = new Size(150, 25) };
            this.Controls.Add(lblName); this.Controls.Add(txtName);

            lblPhone = new Label() { Text = "전화번호", Location = new Point(xLabel, yStart + step * 3), AutoSize = true };
            txtPhone = new TextBox() { Location = new Point(xText, yStart + step * 3 - 3), Size = new Size(150, 25) };
            this.Controls.Add(lblPhone); this.Controls.Add(txtPhone);

            lblCard = new Label() { Text = "카드번호", Location = new Point(xLabel, yStart + step * 4), AutoSize = true };
            txtCard = new TextBox() { Location = new Point(xText, yStart + step * 4 - 3), Size = new Size(150, 25) };
            this.Controls.Add(lblCard); this.Controls.Add(txtCard);

            btnSubmit = new Button();
            btnSubmit.Text = "가입완료";
            btnSubmit.Location = new Point(80, 250);
            btnSubmit.Size = new Size(120, 40);
            btnSubmit.Click += new EventHandler(btnSubmit_Click);
            this.Controls.Add(btnSubmit);
        }

        // --- 기능 구현 ---
        private void btnSubmit_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtNewID.Text) || string.IsNullOrEmpty(txtNewPW.Text))
            {
                MessageBox.Show("필수 정보를 입력하세요.");
                return;
            }

            try
            {
                // 보안 처리
                string salt = Security.GenerateSalt(); //Salt 생성 
                string hashedPassword = Security.HashPassword(txtNewPW.Text, salt); // PW + Salt 해시

                // 승인여부 컬럼 
                // DB에 해시된 비밀번호와 Salt를 함께 저장
                string sql = $@"INSERT INTO 회원 (회원번호, 회원이름, 휴대전화, 등급, 카드번호, 비밀번호, Salt, 승인여부) 
                                VALUES (
                                    '{txtNewID.Text}', 
                                    '{txtName.Text}', 
                                    '{txtPhone.Text}', 
                                    'SILVER', 
                                    '{txtCard.Text}', 
                                    '{hashedPassword}', 
                                    '{salt}',
                                    'N' 
                                )";

                db.ExecuteQuery(sql);

                MessageBox.Show("회원가입 신청이 완료되었습니다.\n관리자 승인 후 로그인 가능합니다.");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("가입 실패: " + ex.Message);
            }
        }
    }
}