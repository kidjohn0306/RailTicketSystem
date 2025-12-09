using System;
using System.Drawing;
using System.Windows.Forms;

namespace RailTicketSystem
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeCustomUI();
        }

        private void InitializeCustomUI()
        {
            this.Text = "메인 메뉴 - 철도 예약 시스템";
            this.Size = new Size(400, 400); 
            this.StartPosition = FormStartPosition.CenterScreen;

            // 환영 문구
            Label lblWelcome = new Label();
            lblWelcome.Text = "환영합니다! 원하시는 메뉴를 선택하세요.";
            lblWelcome.Location = new Point(50, 30);
            lblWelcome.AutoSize = true;
            lblWelcome.Font = new Font("맑은 고딕", 12, FontStyle.Bold);
            this.Controls.Add(lblWelcome);

            // 1. 승차권 예매 버튼
            Button btnBook = new Button();
            btnBook.Text = "승차권 예매";
            btnBook.Location = new Point(100, 80);
            btnBook.Size = new Size(180, 50);
            btnBook.Click += BtnBook_Click;
            this.Controls.Add(btnBook);

            // 2. 예매 확인/취소 버튼
            Button btnCheck = new Button();
            btnCheck.Text = "예매 확인 / 취소";
            btnCheck.Location = new Point(100, 150);
            btnCheck.Size = new Size(180, 50);
            btnCheck.Click += BtnCheck_Click;
            this.Controls.Add(btnCheck);

            // 3. 내 정보 관리 버튼 수정/탈퇴
            Button btnMyPage = new Button();
            btnMyPage.Text = "내 정보 관리 (수정/탈퇴)";
            btnMyPage.Location = new Point(100, 220); 
            btnMyPage.Size = new Size(180, 50);
            btnMyPage.BackColor = Color.LightYellow;
            btnMyPage.Click += BtnMyPage_Click;
            this.Controls.Add(btnMyPage);
        }

        // 승차권 예매 버튼 눌렀을 때
        private void BtnBook_Click(object sender, EventArgs e)
        {
            ReservationForm resForm = new ReservationForm();
            resForm.ShowDialog();
        }

        // 예매 확인/취소 버튼 눌렀을 때
        private void BtnCheck_Click(object sender, EventArgs e)
        {
            CheckForm checkForm = new CheckForm();
            checkForm.ShowDialog();
        }

        private void BtnMyPage_Click(object sender, EventArgs e)
        {
            MyPageForm myPage = new MyPageForm();
            myPage.ShowDialog();
        }
    }
}