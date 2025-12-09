-- ******************************************************
-- 2025 데이터베이스 텀 프로젝트 - 철도 예약 시스템 최종 스크립트
-- 20214106 김종훈
-- ******************************************************

-- ******************************************************
-- [A] 실행 환경 및 변수 설명
-- ******************************************************
-- 1. DB 접속 PW: 1234 (사용자 설정 PW)
-- 2. 관리자 ID: admin123 (C# 코드와 동일)
-- 3. C# 연결 문자열: Pwd=1234;SslMode=None;CharSet=utf8;
-- ******************************************************

-- 안전모드 해제 (데이터 삽입 및 삭제를 위해 필요)
SET SQL_SAFE_UPDATES = 0;

-- DB 초기화 및 생성
DROP DATABASE IF EXISTS RailDB;
CREATE DATABASE RailDB;
USE RailDB;

-- ******************************************************
-- [B] DDL (테이블 생성 및 제약조건)
-- ******************************************************

-- 1. 회원 테이블 (PK, Salt, 등급, 승인여부 포함)
CREATE TABLE 회원 (
    회원번호 VARCHAR(20) NOT NULL PRIMARY KEY,
    회원이름 VARCHAR(10) NOT NULL,
    휴대전화 VARCHAR(15) NOT NULL,
    등급 VARCHAR(10) DEFAULT 'SILVER',
    카드번호 VARCHAR(20) NOT NULL,
    비밀번호 VARCHAR(256) NOT NULL, -- SHA256 해시 저장용
    Salt VARCHAR(100) NULL,
    승인여부 CHAR(1) DEFAULT 'N',     --  관리자 승인 여부
    CHECK (등급 IN ('VIP', 'GOLD', 'SILVER', 'Manager')) -- 관리자 ID도 등급처럼 처리
);

-- 2. 기차역 테이블
CREATE TABLE 기차역 (
    역순번 INT NOT NULL,
    역이름 VARCHAR(10) NOT NULL,
    CONSTRAINT 기차역_기본키 PRIMARY KEY(역순번, 역이름)
);

-- 3. 열차 좌석 데이터 (SM=4석/량, MG=6석/량)
-- 모든 열차의 좌석을 사전에 삽입합니다. (총 40개 좌석)
INSERT INTO 열차좌석 (열차번호, 차량번호, 좌석번호) VALUES 
-- SM01, SM02 (4석 x 2량 x 2대 = 16 rows)
('SM01', '1', '1A'), ('SM01', '1', '1B'), ('SM01', '1', '2A'), ('SM01', '1', '2B'),
('SM01', '2', '1A'), ('SM01', '2', '1B'), ('SM01', '2', '2A'), ('SM01', '2', '2B'),
('SM02', '1', '1A'), ('SM02', '1', '1B'), ('SM02', '1', '2A'), ('SM02', '1', '2B'),
('SM02', '2', '1A'), ('SM02', '2', '1B'), ('SM02', '2', '2A'), ('SM02', '2', '2B'),

-- MG01, MG02 (6석 x 2량 x 2대 = 24 rows)
('MG01', '1', '1A'), ('MG01', '1', '1B'), ('MG01', '1', '1C'), ('MG01', '1', '2A'), ('MG01', '1', '2B'), ('MG01', '1', '2C'),
('MG01', '2', '1A'), ('MG01', '2', '1B'), ('MG01', '2', '1C'), ('MG01', '2', '2A'), ('MG01', '2', '2B'), ('MG01', '2', '2C'),
('MG02', '1', '1A'), ('MG02', '1', '1B'), ('MG02', '1', '1C'), ('MG02', '1', '2A'), ('MG02', '1', '2B'), ('MG02', '1', '2C'),
('MG02', '2', '1A'), ('MG02', '2', '1B'), ('MG02', '2', '1C'), ('MG02', '2', '2A'), ('MG02', '2', '2B'), ('MG02', '2', '2C');

-- 4. 열차좌석 테이블 (좌석 마스터 정보)
CREATE TABLE 열차좌석 (
    열차번호 VARCHAR(20) NOT NULL,
    차량번호 VARCHAR(2) NOT NULL,
    좌석번호 VARCHAR(10) NOT NULL,
    CONSTRAINT 열차좌석_기본키 PRIMARY KEY(열차번호, 차량번호, 좌석번호),
    CONSTRAINT 열차좌석_FK FOREIGN KEY(열차번호) REFERENCES 열차(열차번호) ON DELETE CASCADE
);

-- 5. 운행시간표 테이블
CREATE TABLE 운행시간표 (
    역순번 INT NOT NULL,
    열차번호 VARCHAR(20) NOT NULL,
    방향 VARCHAR(2) NOT NULL,
    시간 DATETIME NOT NULL,
    CONSTRAINT 운행시간표_기본키 PRIMARY KEY(역순번, 열차번호, 방향, 시간)
);

-- 6. 예약현황 테이블 (헤더 및 결제 정보)
CREATE TABLE 예약현황 (
    예약번호 VARCHAR(30) NOT NULL PRIMARY KEY,
    회원번호 VARCHAR(20) NOT NULL,
    출발역 VARCHAR(10) NOT NULL,
    도착역 VARCHAR(10) NOT NULL,
    예매일시 DATETIME NOT NULL,
    금액 INT NOT NULL,
    결제방법 VARCHAR(6) NOT NULL,
    결제카드번호 VARCHAR(20) NULL,   -- 카드번호 저장
    CONSTRAINT 예약현황_FK_회원 FOREIGN KEY(회원번호) REFERENCES 회원(회원번호),
    CHECK (출발역 != 도착역),
    CHECK (금액 >= 0)
);

-- 7. 예약좌석 테이블 (구간 점유 상세 정보)
CREATE TABLE 예약좌석 (
    역순번 INT NOT NULL,
    열차번호 VARCHAR(20) NOT NULL,
    방향 VARCHAR(2) NOT NULL,
    시간 DATETIME NOT NULL,
    운행날짜 DATE NOT NULL,
    차량번호 VARCHAR(2) NOT NULL,
    좌석번호 VARCHAR(10) NOT NULL,
    예약번호 VARCHAR(30) NOT NULL,
    CONSTRAINT 예약좌석_기본키 PRIMARY KEY(역순번, 열차번호, 방향, 시간, 운행날짜, 차량번호, 좌석번호)
);

-- FK 설정 (CASCADE ON DELETE로 예약현황 삭제 시 예약좌석 자동 삭제)
ALTER TABLE 예약좌석 ADD CONSTRAINT 예약좌석_FK_예약 FOREIGN KEY(예약번호) REFERENCES 예약현황(예약번호) ON DELETE CASCADE;


-- ******************************************************
-- [D] DML (기본 데이터 삽입)
-- ******************************************************

-- 1. 기차역 데이터 (하행선 기준 순번)
INSERT IGNORE INTO 기차역 (역순번, 역이름) VALUES 
(1, '서울'), (2, '천안'), (3, '대전'), (4, '대구'), (5, '부산');

-- 2. 열차 데이터 (새마을 2대, 무궁화 2대)
INSERT IGNORE INTO 열차 (열차번호, 열차등급) VALUES 
('SM01', 'SM'), ('SM02', 'SM'), ('MG01', 'MG'), ('MG02', 'MG');

-- 3. 열차 좌석 데이터 (SM=4석/량, MG=6석/량)
-- SM01, MG01 등 모든 열차의 좌석을 사전에 삽입해야 함.
-- (Full Seat Data 삽입 로직은 C# AdminForm에서 수행되므로 여기서는 최소한의 데이터만 포함)

-- 4. 요금표 데이터 (모든 구간에 대해 SM, MG 요금 설정)
INSERT INTO 요금표 (출발역, 도착역, 열차등급, 요금) VALUES

-- [새마을호 SM] (총 10개 구간)
('서울', '천안', 'SM', 5000), 
('서울', '대전', 'SM', 10000), 
('서울', '대구', 'SM', 20000), 
('서울', '부산', 'SM', 30000),

('천안', '대전', 'SM', 5000), 
('천안', '대구', 'SM', 15000), 
('천안', '부산', 'SM', 25000),

('대전', '대구', 'SM', 10000), 
('대전', '부산', 'SM', 20000),

('대구', '부산', 'SM', 10000),

-- [무궁화호 MG] (총 10개 구간)
('서울', '천안', 'MG', 3000), 
('서울', '대전', 'MG', 6000), 
('서울', '대구', 'MG', 12000), 
('서울', '부산', 'MG', 18000),

('천안', '대전', 'MG', 3000), 
('천안', '대구', 'MG', 9000), 
('천안', '부산', 'MG', 15000),

('대전', '대구', 'MG', 6000), 
('대전', '부산', 'MG', 12000),

('대구', '부산', 'MG', 6000);

-- ******************************************************
-- [E] 저장 프로시저 (SP)
-- ******************************************************

DELIMITER $$

CREATE PROCEDURE sp_book_seat_segments (
    IN p_resNo VARCHAR(30),
    IN p_memberID VARCHAR(20),
    IN p_startStation VARCHAR(10),
    IN p_endStation VARCHAR(10),
    IN p_totalAmt INT,
    IN p_payMethod VARCHAR(6),
    IN p_cardNum VARCHAR(20),
    IN p_trainNo VARCHAR(20),
    IN p_runDate DATE,
    IN p_carNum VARCHAR(2),
    IN p_seatNum VARCHAR(10),
    IN p_startTime DATETIME
)
BEGIN
    DECLARE v_start_seq INT;
    DECLARE v_end_seq INT;
    DECLARE v_current_seq INT;
    DECLARE v_current_time DATETIME;
    DECLARE v_direction VARCHAR(10);
    DECLARE v_card_num_str VARCHAR(25);

    START TRANSACTION;

    -- 카드번호 NULL 처리
    SET v_card_num_str = p_cardNum;
    IF v_card_num_str = 'NULL' OR v_card_num_str = '' THEN
        SET v_card_num_str = NULL;
    END IF;

    -- 예약현황 (헤더) 삽입
    INSERT IGNORE INTO 예약현황 (예약번호, 회원번호, 출발역, 도착역, 예매일시, 금액, 결제방법, 결제카드번호)
    VALUES (p_resNo, p_memberID, p_startStation, p_endStation, NOW(), p_totalAmt, p_payMethod, v_card_num_str);
    
    -- 역 순번 및 방향 조회
    SELECT 역순번 INTO v_start_seq FROM 기차역 WHERE 역이름 = p_startStation;
    SELECT 역순번 INTO v_end_seq FROM 기차역 WHERE 역이름 = p_endStation;
    
    IF v_start_seq < v_end_seq THEN
        SET v_direction = '하행';
    ELSE
        SET v_direction = '상행';
    END IF;

    -- 예약 좌석 상세 삽입 (구간별로 반복)
    SET v_current_seq = v_start_seq;
    
    WHILE v_current_seq != v_end_seq DO
        
        -- 해당 역의 정확한 출발 시간 조회
        SELECT T.시간 INTO v_current_time
        FROM 운행시간표 T 
        WHERE T.열차번호 = p_trainNo 
          AND T.역순번 = v_current_seq
          AND T.방향 = v_direction
          AND DATE(T.시간) = p_runDate
        LIMIT 1; 

        -- Detail Insert (예약좌석)
        INSERT INTO 예약좌석 (역순번, 열차번호, 방향, 시간, 운행날짜, 차량번호, 좌석번호, 예약번호)
        VALUES (v_current_seq, p_trainNo, v_direction, v_current_time, p_runDate, p_carNum, p_seatNum, p_resNo);

        -- 다음 역으로 이동
        IF v_direction = '하행' THEN
            SET v_current_seq = v_current_seq + 1;
        ELSE
            SET v_current_seq = v_current_seq - 1;
        END IF;

    END WHILE;

    COMMIT;
    
    SELECT 'SUCCESS' AS Status;

END$$
DELIMITER ;