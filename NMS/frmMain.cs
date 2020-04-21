//Ver 1.01(2015년 09월 04일 ~
//  1. 소스텔의 FM 단독형장치 추가
//
//Ver 1.02(2015년 09월 10일 ~
//  1. 분당선 추가
//
//Ver 1.03(2015년 09월 14일 ~
//  1. 경의일산선 추가
//
//--2015년 09월 17일--
//  2. 임계값 적용
//     - 송신출력과 DC 전압에 대해서 임계치를 설정하고 설정된 범위를 벗어나면 알람창과 알람을 울리도록 함.
//
//Ver 1.04(2015년 09월 18일 ~
//  1. DB 검색시 각 노선에 맞게 검색 및 출력하도록 함.
//     - 분당선(1~5번기지국)은 검색결과에 무선수신기(감청) 관련 고장정보와 DC 고장정보가 없음.
//     - 과천선, 분당선(6번기지국 ~)는 검색결과에 무선수신기(감청) 관련 고장정보와 DC 고장정보가 포함되어 있음.
//     - 위 두종류로 검색 기능을 분류
//
//     - 경의일산선 : 경의선은 분당선(1~5번기지국)과 같음.
//                    일산선은 DB Table부터 DB 저장/검색등의 기능들을 새로 만들어야함.
//
//Ver 1.05(2015년 10월 05일 ~
//  1. 수인선 추가
//
//
//

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using System.IO;
using System.Collections;

using System.Net;
using System.Net.Sockets;

namespace NMS
{
    public partial class frmMain : Form
    {
        private Common.frmDataView frmDV = null;

        private Common.frmPassword frmPW = null;
        private int pwApplyIndex = 0;

        private Common.clsNMSSendDataMake nmsSendDataMake = new Common.clsNMSSendDataMake();

        #region 컨트롤 배열
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        //공통
        private List<Button> btCommMenu = new List<Button>();
        private List<Panel> panelGround = new List<Panel>();

        //주화면
        private List<Panel> panelMainBase = new List<Panel>();

        //전체 감시화면 관련
        private List<Button> btMu = new List<Button>();
        private List<Button> btRuA = new List<Button>();
        private Button[,] btRuB = new Button[99, 4];

        private List<Button> btMuFm = new List<Button>();
        private List<Button> btRuAFm = new List<Button>();
        private Button[,] btRuBFm = new Button[99, 4];

        private List<Button> btConnectServer = new List<Button>();
        private List<Label> lblConnectServer = new List<Label>();
        private List<Label> lblRxServer = new List<Label>();
        private List<Label> lblTxServer = new List<Label>();

        //설정화면
        //MU/RU 이름 및 기타설정 화면
        private List<TextBox> tbRuName = new List<TextBox>();

        private List<CheckBox> chkLif = new List<CheckBox>();

        //A/S용 AF Gain 관련
        private List<Label> lblCCEAfGain = new List<Label>();
        private List<Label> lblCCE2AfGain = new List<Label>();
        private List<Label> lblRC1AfGain = new List<Label>();
        private List<Label> lblRC2AfGain = new List<Label>();
        private List<Label> lblRC3AfGain = new List<Label>();
        private List<Label> lblRC4AfGain = new List<Label>();

        private List<TextBox> tbCCEAfGain = new List<TextBox>();
        private List<TextBox> tbCCE2AfGain = new List<TextBox>();
        private List<TextBox> tbRC1AfGain = new List<TextBox>();
        private List<TextBox> tbRC2AfGain = new List<TextBox>();
        private List<TextBox> tbRC3AfGain = new List<TextBox>();
        private List<TextBox> tbRC4AfGain = new List<TextBox>();

        private List<Button> btAfGainSet = new List<Button>();
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        private string nowStation = string.Empty;

        private Color colorSelect = new Color();    //선택 색
        private Color colorError = new Color();    //선택 색
        private Color colorBase = new Color();      //기본 색
        
        #region MU/RU 상태 저장 변수
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private Common.MURUData[] muruNowData = null;
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        private Common.CommErrCheck[] commErrCheck = null; //주/예비/감청 통신 Error를 Check하기 위한 변수

        //MU/RU Data 변경된것을 감지하기 위한 변수
        private Common.MURUDataChange[] mrurDataChange = null;

        #region Database 관련 변수
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private int day = 0;  //DB 파일을 하루에 하나씩 만든다.

        private Common.nmsDBMS fbDBMS_NMS = null;

        private Queue dbMuQueue = new Queue();
        private Queue dbMuQueue_ILSAN = new Queue();
        private Queue dbMuFmQueue = new Queue();
        private Queue dbRuAQueue = new Queue();
        private Queue dbRuBQueue = new Queue();
        private Queue dbRuFmQueue = new Queue();

        private Common.clsStatusListDBMS fbDBMS_StatusListInfo = new Common.clsStatusListDBMS();  //현재 상황 저장
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        private byte[] cntServerPolling = new byte[2];  //NMS Server MAIN/STBY와의 통신 여부
        private bool[] flagServerPolling = new bool[2];
        private byte serverSt = 0;

        bool flagILSAN = false;     //일산선 MU를 선택했는지의 여부, MU초기화 및 상태 표시시 사용

        Common.frmErrorMessage frmErrMsg = new Common.frmErrorMessage();

        public frmMain()
        {
            StreamReader fr = null;

            #region 파일로 부터 설정 정보 읽어오기
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            try
            {
                fr = File.OpenText(Common.clsCommon.DefaultPath + "setup.txt");

                Common.clsCommon.thisVer = fr.ReadLine();      //Version Read

                int tmpVer = Common.clsCommon.VerCheck_NMS(Common.clsCommon.thisVer);

                switch (tmpVer)
                {
                    case 0:     //Ver 1.00 ~ 
                        Common.clsNMS.nmsGUIUser = fr.ReadLine();     //사용처

                        //NMS 주장치 정보
                        Common.clsNMS.nmsServerIP[0] = fr.ReadLine();
                        Common.clsNMS.nmsServerIP[1] = fr.ReadLine();
                        Common.clsNMS.nmsPort = Convert.ToInt16(fr.ReadLine());

                        //경보 관련 정보
                        if (fr.ReadLine() == "True") Common.clsNMS.flagSoundAction = true;  //경보 재생 유무
                        else Common.clsNMS.flagSoundAction = false;
                        Common.clsNMS.bellPlayCycle = Convert.ToInt32(fr.ReadLine());  //경보음 재생 주기
                        Common.clsNMS.bellFile = fr.ReadLine();     //경보음 파일
                        
                        Common.clsCommon.ServerPath = fr.ReadLine();  //Database Path 정보
                        break;

                    default:
                        //if (clsCommon.flagLanguage) MessageBox.Show("프로그램 버젼이 맞지 않습니다. 확인후 다시 시도하시기 바랍니다.");
                        //else MessageBox.Show("Program version does not match. Try again after verification.");

                        this.Close();
                        break;
                }
            }
            catch
            {
            }

            fr.Close();
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion

            #region 파일로 부터 역 List 정보 읽어오기
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                    try
                    {
                        string fileName = @"\\" + Common.clsNMS.nmsServerIP[0] + @"\NMS Server\과천선_Server.txt";
                        fr = File.OpenText(fileName);

                        string tmpStation = fr.ReadLine();

                        while (tmpStation != null)
                        {
                            Common.clsNMS.stationList.Add(tmpStation);
                            tmpStation = fr.ReadLine();
                        }
                    }
                    catch
                    {
                        try
                        {
                            string fileName = @"\\" + Common.clsNMS.nmsServerIP[1] + @"\NMS Server\과천선_Server.txt";
                            fr = File.OpenText(fileName);

                            string tmpStation = fr.ReadLine();

                            while (tmpStation != null)
                            {
                                Common.clsNMS.stationList.Add(tmpStation);
                                tmpStation = fr.ReadLine();
                            }
                        }
                        catch
                        {
                        }
                    }
                    break;

                case "분당선":
                case "경의일산선":
                case "수인선":
                    try
                    {
                        fr = File.OpenText(Common.clsCommon.DefaultPath + Common.clsNMS.nmsGUIUser + ".txt");

                        string tmpStation = fr.ReadLine();

                        while (tmpStation != null)
                        {
                            Common.clsNMS.stationList.Add(tmpStation);
                            tmpStation = fr.ReadLine();
                        }
                    }
                    catch
                    {
                    }
                    break;
            }

            fr.Close();
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                case "분당선":
                case "수인선":
                    InitializeComponent();
                    break;

                case "경의일산선":
                    InitializeComponent_1280_1024();
                    break;

                default:
                    InitializeComponent();
                    break;
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            #region 컨트롤 배열 초기화
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            //공통
            //private List<Button> btCommMenu = new List<Button>();
            btCommMenu.Add(btMenu1); btCommMenu.Add(btMenu2); btCommMenu.Add(btMenu3); btCommMenu.Add(btMenu4);
            //private List<Panel> panelGround = new List<Panel>();
            panelGround.Add(panelGround1); panelGround.Add(panelGround2); panelGround.Add(panelGround3);

            //주화면
            //private List<Panel> panelMainBase = new List<Panel>();
            panelMainBase.Add(panelMainBase1); panelMainBase.Add(panelMainBase2);

            //전체 감시화면 관련
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                case "과천선 Server":
                    SetVisible(panel과천선, true);
                    SetVisible(panel분당선, false);
                    SetVisible(panel경의일산선, false);
                    SetVisible(panel수인선, false);

                    //private List<Button> btMu = new List<Button>();
                    btMu.Add(btGC_MU1); btMu.Add(btGC_MU2); btMu.Add(btGC_MU3);
                    //private List<Button> btRuA = new List<Button>();
                    btRuA.Add(btGC_RU1); btRuA.Add(btGC_RU2); btRuA.Add(btGC_RU3);
                    //private Button[,] btRuB = new Button[4, 4];
                    btRuB[0, 0] = btGC_RU1_1; btRuB[0, 1] = btGC_RU1_2;
                    btRuB[1, 0] = btGC_RU2_1;
                    btRuB[2, 0] = btGC_RU3_1; btRuB[2, 1] = btGC_RU3_2;

                    //private List<Button> btMuFm = new List<Button>();
                    btMuFm.Add(btMUFM1); btMuFm.Add(btMUFM2); btMuFm.Add(btMUFM3);
                    //private List<Button> btRuAFm = new List<Button>();
                    btRuAFm.Add(btGC_RUFM1); btRuAFm.Add(btGC_RUFM2); btRuAFm.Add(btGC_RUFM3);
                    //private Button[,] btRuBFm = new Button[4, 4];
                    btRuBFm[0, 0] = btGC_RUFM1_1; btRuBFm[0, 1] = btGC_RUFM1_2;
                    btRuBFm[1, 0] = btGC_RUFM2_1;
                    btRuBFm[2, 0] = btGC_RUFM3_1; btRuBFm[2, 1] = btGC_RUFM3_2;

                    //private List<Button> btConnectServer = new List<Button>();
                    btConnectServer.Add(btConnectServer1); btConnectServer.Add(btConnectServer2);
                    //private List<Label> lblConnectServer = new List<Label>();
                    lblConnectServer.Add(lblConnectServer1); lblConnectServer.Add(lblConnectServer2);
                    //private List<Label> lblRxServer = new List<Label>();
                    lblRxServer.Add(lblRxServer1); lblRxServer.Add(lblRxServer2);
                    //private List<Label> lblTxServer = new List<Label>();
                    lblTxServer.Add(lblTxServer1); lblTxServer.Add(lblTxServer2);
                    break;

                case "분당선":
                    SetVisible(panel과천선, false);
                    SetVisible(panel분당선, true);
                    SetVisible(panel경의일산선, false);
                    SetVisible(panel수인선, false);

                    //private List<Button> btMu = new List<Button>();
                    btMu.Add(btMU1); btMu.Add(btMU2); btMu.Add(btMU3); btMu.Add(btMU4); btMu.Add(btMU5);
                    btMu.Add(btMU6); btMu.Add(btMU7); btMu.Add(btMU8); btMu.Add(btMU9); btMu.Add(btMU10);
                    btMu.Add(btMU11); btMu.Add(btMU12); btMu.Add(btMU13);
                    //private List<Button> btRuA = new List<Button>();
                    btRuA.Add(btRU1); btRuA.Add(btRU2); btRuA.Add(btRU3); btRuA.Add(btRU4); btRuA.Add(btRU5);
                    btRuA.Add(btRU6); btRuA.Add(btRU7); btRuA.Add(btRU8); btRuA.Add(btRU9); btRuA.Add(btRU10);
                    btRuA.Add(btRU11); btRuA.Add(btRU12); btRuA.Add(btRU13);
                    //private Button[,] btRuB = new Button[4, 4];
                    btRuB[0, 0] = btRU1_1;
                    btRuB[1, 0] = btRU2_1; btRuB[1, 1] = btRU2_2;
                    btRuB[3, 0] = btRU4_1; btRuB[3, 1] = btRU4_2;
                    btRuB[5, 0] = btRU6_1; btRuB[5, 1] = btRU6_2; btRuB[5, 2] = btRU6_3;
                    btRuB[6, 0] = btRU7_1; btRuB[6, 1] = btRU7_2;
                    btRuB[7, 0] = btRU8_1;
                    btRuB[8, 0] = btRU9_1;
                    btRuB[9, 0] = btRU10_1;
                    btRuB[10, 0] = btRU11_1;
                    btRuB[11, 0] = btRU12_1; btRuB[11, 1] = btRU12_2;

                    //private List<Button> btRuAFm = new List<Button>();
                    btRuAFm.Add(btRUFM1); btRuAFm.Add(btRUFM2); btRuAFm.Add(btRUFM3); btRuAFm.Add(btRUFM4); btRuAFm.Add(btRUFM5);
                    btRuAFm.Add(btRUFM6); btRuAFm.Add(btRUFM7); btRuAFm.Add(btRUFM8); btRuAFm.Add(btRUFM9); btRuAFm.Add(btRUFM10);
                    btRuAFm.Add(btRUFM11); btRuAFm.Add(btRUFM12); btRuAFm.Add(btRUFM13);
                    //private Button[,] btRuBFm = new Button[4, 4];
                    btRuBFm[0, 0] = btRUFM1_1;
                    btRuBFm[1, 0] = btRUFM2_1; btRuBFm[1, 1] = btRUFM2_2;
                    btRuBFm[3, 0] = btRUFM4_1; btRuBFm[3, 1] = btRUFM4_2;
                    btRuBFm[5, 0] = btRUFM6_1; btRuBFm[5, 1] = btRUFM6_2; btRuBFm[5, 2] = btRUFM6_3;
                    btRuBFm[6, 0] = btRUFM7_1; btRuBFm[6, 1] = btRUFM7_2;
                    btRuBFm[7, 0] = btRUFM8_1;
                    btRuBFm[8, 0] = btRUFM9_1;
                    btRuBFm[9, 0] = btRUFM10_1;
                    btRuBFm[10, 0] = btRUFM11_1;
                    btRuBFm[11, 0] = btRUFM12_1; btRuBFm[11, 1] = btRUFM12_2;
                    break;

                case "경의일산선":
                    SetVisible(panel과천선, false);
                    SetVisible(panel분당선, false);
                    SetVisible(panel경의일산선, true);
                    SetVisible(panel수인선, false);

                    //전체 감시화면 관련
                    //private List<Button> btMu = new List<Button>();
                    btMu.Add(btKUMU1); btMu.Add(btKUMU2); btMu.Add(btISMU1); btMu.Add(btISMU2); btMu.Add(btISMU3); btMu.Add(btISMU4);
                    //private List<Button> btMu = new List<Button>();
                    btMuFm.Add(btKUFM1); btMuFm.Add(btKUFM2); btMuFm.Add(btISFM1); btMuFm.Add(btISFM2); btMuFm.Add(btISFM3); btMuFm.Add(btISFM4);

                    //private List<Button> btRuA = new List<Button>();
                    btRuA.Add(btKURU1); btRuA.Add(btKURU2);
                    //private List<Button> btRuAFm = new List<Button>();
                    btRuAFm.Add(btKUFM1); btRuAFm.Add(btKUFM2);
                    //private Button[,] btRuB = new Button[4, 4];
                    btRuB[0, 0] = btKURU1_1;
                    btRuB[1, 0] = btKURU2_1; btRuB[1, 1] = btKURU2_2; btRuB[1, 2] = btKURU2_3;
                    //private Button[,] btRuBFm = new Button[4, 4];
                    btRuBFm[0, 0] = btKUFM1_1;
                    btRuBFm[1, 0] = btKUFM2_1; btRuBFm[1, 1] = btKUFM2_2; btRuBFm[1, 2] = btKUFM2_3;
                    break;

                case "수인선":
                    SetVisible(panel과천선, false);
                    SetVisible(panel분당선, false);
                    SetVisible(panel경의일산선, false);
                    SetVisible(panel수인선, true);

                    //private List<Button> btMu = new List<Button>();
                    btMu.Add(btSI_MU1); btMu.Add(btSI_MU2);
                    //private List<Button> btRuA = new List<Button>();
                    btRuA.Add(btSI_RU1); btRuA.Add(btSI_RU2);
                    //private Button[,] btRuB = new Button[4, 4];
                    btRuB[0, 0] = btSI_RU1_1;
                    btRuB[1, 0] = btSI_RU2_1;

                    //private List<Button> btMuFm = new List<Button>();
                    btMuFm.Add(btSI_MUFM1); btMuFm.Add(btSI_MUFM2);
                    //private List<Button> btRuAFm = new List<Button>();
                    btRuAFm.Add(btSI_RUFM1); btRuAFm.Add(btSI_RUFM2);
                    //private Button[,] btRuBFm = new Button[4, 4];
                    btRuBFm[0, 0] = btSI_RUFM1_1;
                    btRuBFm[1, 0] = btSI_RUFM2_1;
                    break;
            }

            //설정화면
            //private List<TextBox> tbRuName = new List<TextBox>();
            tbRuName.Add(tbRuName1); tbRuName.Add(tbRuName2); tbRuName.Add(tbRuName3); tbRuName.Add(tbRuName4);

            //private List<CheckBox> chkLif = new List<CheckBox>();
            chkLif.Add(chkLif1); chkLif.Add(chkLif2); chkLif.Add(chkLif3); chkLif.Add(chkLif4); chkLif.Add(chkLif5); chkLif.Add(chkLif6);

            //AF Gain 관련
            //private ArrayList lblCCEAfGain = new ArrayList();
            lblCCEAfGain.Add(lblCCEAfGain1); lblCCEAfGain.Add(lblCCEAfGain2); lblCCEAfGain.Add(lblCCEAfGain3);
            //private ArrayList lblCCE2AfGain = new ArrayList();
            lblCCE2AfGain.Add(lblCCE2AfGain1); lblCCE2AfGain.Add(lblCCE2AfGain2); lblCCE2AfGain.Add(lblCCE2AfGain3);
            //private ArrayList lblRC1AfGain = new ArrayList();
            lblRC1AfGain.Add(lblRC1AfGain1); lblRC1AfGain.Add(lblRC1AfGain2); lblRC1AfGain.Add(lblRC1AfGain3);
            //private ArrayList lblRC2AfGain = new ArrayList();
            lblRC2AfGain.Add(lblRC2AfGain1); lblRC2AfGain.Add(lblRC2AfGain2); lblRC2AfGain.Add(lblRC2AfGain3);
            //private ArrayList lblRC3AfGain = new ArrayList();
            lblRC3AfGain.Add(lblRC3AfGain1); lblRC3AfGain.Add(lblRC3AfGain2); lblRC3AfGain.Add(lblRC3AfGain3);
            //private ArrayList lblRC4AfGain = new ArrayList();
            lblRC4AfGain.Add(lblRC4AfGain1); lblRC4AfGain.Add(lblRC4AfGain2); lblRC4AfGain.Add(lblRC4AfGain3);

            //private ArrayList tbCCEAfGain = new ArrayList();
            tbCCEAfGain.Add(tbCCEAfGain1); tbCCEAfGain.Add(tbCCEAfGain2); tbCCEAfGain.Add(tbCCEAfGain3);
            //private ArrayList tbCCE2AfGain = new ArrayList();
            tbCCE2AfGain.Add(tbCCE2AfGain1); tbCCE2AfGain.Add(tbCCE2AfGain2); tbCCE2AfGain.Add(tbCCE2AfGain3);
            //private ArrayList tbRC1AfGain = new ArrayList();
            tbRC1AfGain.Add(tbRC1AfGain1); tbRC1AfGain.Add(tbRC1AfGain2); tbRC1AfGain.Add(tbRC1AfGain3);
            //private ArrayList tbRC2AfGain = new ArrayList();
            tbRC2AfGain.Add(tbRC2AfGain1); tbRC2AfGain.Add(tbRC2AfGain2); tbRC2AfGain.Add(tbRC2AfGain3);
            //private ArrayList tbRC3AfGain = new ArrayList();
            tbRC3AfGain.Add(tbRC3AfGain1); tbRC3AfGain.Add(tbRC3AfGain2); tbRC3AfGain.Add(tbRC3AfGain3);
            //private ArrayList tbRC4AfGain = new ArrayList();
            tbRC4AfGain.Add(tbRC4AfGain1); tbRC4AfGain.Add(tbRC4AfGain2); tbRC4AfGain.Add(tbRC4AfGain3);

            //private ArrayList btAfGainSet = new ArrayList();
            btAfGainSet.Add(btAfGainSet1); btAfGainSet.Add(btAfGainSet2); btAfGainSet.Add(btAfGainSet3); btAfGainSet.Add(btAfGainSet4);
            btAfGainSet.Add(btAfGainSet5); btAfGainSet.Add(btAfGainSet6); btAfGainSet.Add(btAfGainSet7);
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion

            int i = 0;

            Common.clsCommon.thisVer = "Ver 1.05";

            this.Text = Common.clsNMS.nmsGUIUser + " 열차무선 NMS - " + Common.clsCommon.thisVer;
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                case "과천선 Server":
                    SetText(lblTitle, "과 천 선   열 차 무 선   N M S");
                    break;

                case "분당선":
                    SetText(lblTitle, "분 당 선   열 차 무 선   N M S");
                    break;

                case "경의일산선":
                    SetText(lblTitle, "경 의 선 / 일 산 선   열 차 무 선   N M S");
                    break;

                case "수인선":
                    SetText(lblTitle, "수 인 선   열 차 무 선   N M S");
                    ucRUSt.SetEnableFM(false);
                    
                    break;
            }

            for (i = 0; i < 4; i++) btCommMenu[i].Tag = i;

            for (i = 0; i < 7; i++) btAfGainSet[i].Tag = i;
            for (i = 0; i < 6; i++) chkLif[i].Tag = i;

            Common.clsCommon.Make_CRC_Table();

            colorSelect = Color.Lime;
            colorError = Color.Red;
            colorBase = Color.Transparent;
            
            #region 임계값 읽어오기
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            StreamReader fr = File.OpenText(Common.clsCommon.DefaultPath + "rangeSetup.txt");

            for (i = 0; i < 4; i++)
                Common.clsNMS.rangeValue[i] = Convert.ToDouble(fr.ReadLine());

            fr.Close();
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion
            
            NMS_Init();

            #region Database 관련 초기화 작업
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            day = DateTime.Now.Day;

            switch(Common.clsNMS.nmsGUIUser )
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    NMSDB_Connect();
                    break;
            }

            //운용정보 저장/검색 관련 DB 초기화
            ImageList dummyImageSearch = new ImageList();
            dummyImageSearch.ImageSize = new System.Drawing.Size(1, 25);

            //lvUseMsg.SmallImageList = dummyImageSearch;
            //lvOperationReport.SmallImageList = dummyImageSearch;    //운용정보 ListView의 높이설정

            //통신내역/장애내역 관련 이력 검색
            //lvTxRxErxStAllDiaplay.SmallImageList = dummyImageSearch;
            //lvMUErrorStAllDiaplay.SmallImageList = dummyImageSearch;
            //lvRUAErrorStAllDiaplay.SmallImageList = dummyImageSearch;
            //lvRUErrorStAllDiaplay.SmallImageList = dummyImageSearch;
            //lvsourcetelBDAErrorStAllDiaplay.SmallImageList = dummyImageSearch;
            //lvsourcetelFMErrorStAllDiaplay.SmallImageList = dummyImageSearch;
            //lvsourcetelFMChildErrorStAllDiaplay.SmallImageList = dummyImageSearch;

            fbDBMS_StatusListInfo.DBConnect(Common.clsCommon.DataBasePath + "statusList_x64.FDB");
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    SetVisible(lblConnectNMS, true);
                    SetVisible(lblRxNMS, true);
                    SetVisible(lblTxNMS, true);
                    break;
            }

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "경의일산선":
                    for (i = 0; i < 3; i++)
                        panelGround[i].BackgroundImage = new Bitmap(Common.clsCommon.ImagePath + "Ground_1252_870_1.png");
                    panelMessageView.BackgroundImage = new Bitmap(Common.clsCommon.ImagePath + "Ground_840_100_0.png");
                    break;
            }

            SetInput();

            btMenu_Click(1);    //주화면 선택

            tmrMain.Start();

            AddStatus("프로그램을 시작합니다.");

            frmErrMsg.Show();
            SetVisible(frmErrMsg, false);
        }

        #region 컨트롤 제어(문자출력, 활성화 변경, 색변경)
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        //컨트롤에 문자를 출력하기 위함
        private void SetTextAdd(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                Common.SetTextCallback d = new Common.SetTextCallback(SetTextAdd);
                this.Invoke(d, new object[] { control, text });
            }
            else
            {
                if (((TextBox)control).Text.Length > 512)
                    ((TextBox)control).Text = "";

                ((TextBox)control).Text += text;

                ((TextBox)control).SelectionStart = ((TextBox)control).Text.Length;//맨 마지막 선택...
                ((TextBox)control).ScrollToCaret();
            }
        }

        //컨트롤에 문자를 출력하기 위함
        private void SetText(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                Common.SetTextCallback d = new Common.SetTextCallback(SetText);
                this.Invoke(d, new object[] { control, text });
            }
            else
            {
                control.Text = text;
            }
        }

        //컨트롤의 활성화 여부를 결정하기 위함
        private void SetEnable(Control control, bool flag)
        {
            if (control.InvokeRequired)
            {
                Common.SetEnableCallback d = new Common.SetEnableCallback(SetEnable);
                this.Invoke(d, new object[] { control, flag });
            }
            else
            {
                control.Enabled = flag;
            }
        }

        //컨트롤에 색을 변경하기 위함
        private void SetColor(Control control, Color color)
        {
            if (control.InvokeRequired)
            {
                Common.SetColorCallback d = new Common.SetColorCallback(SetColor);
                this.Invoke(d, new object[] { control, color });
            }
            else
            {
                control.BackColor = color;
            }
        }

        //컨트롤의 글자색을 변경하기 위함
        private void SetForeColor(Control control, Color color)
        {
            if (control.InvokeRequired)
            {
                Common.SetColorCallback d = new Common.SetColorCallback(SetForeColor);
                this.Invoke(d, new object[] { control, color });
            }
            else
            {
                control.ForeColor = color;
            }
        }

        //컨트롤의 Visible값을 변경하기 위함
        private void SetVisible(Control control, bool flag)
        {
            if (control.InvokeRequired)
            {
                Common.SetVisibleCallback d = new Common.SetVisibleCallback(SetVisible);
                this.Invoke(d, new object[] { control, flag });
            }
            else
            {
                control.Visible = flag;
            }
        }

        //컨트롤에 체크하기 위함
        private void SetChecked(CheckBox control, bool flag)
        {
            if (control.InvokeRequired)
            {
                Common.SetCheckedCallback d = new Common.SetCheckedCallback(SetChecked);
                this.Invoke(d, new object[] { control, flag });
            }
            else
            {
                control.Checked = flag;
            }
        }

        //컨트롤에 체크하기 위함
        private void SetRdButChecked(RadioButton control, bool flag)
        {
            if (control.InvokeRequired)
            {
                Common.SetRdButCheckedCallback d = new Common.SetRdButCheckedCallback(SetRdButChecked);
                this.Invoke(d, new object[] { control, flag });
            }
            else
            {
                control.Checked = flag;
            }
        }

        //컨트롤의 Value값을 변경하기 위함
        private void SetValue(ProgressBar control, int maxValue, int value)
        {
            if (control.InvokeRequired)
            {
                Common.SetValueCallback d = new Common.SetValueCallback(SetValue);
                this.Invoke(d, new object[] { control, maxValue, value });
            }
            else
            {
                control.Maximum = maxValue;
                control.Value = value;
            }
        }

        //TrackBar 컨트롤에 value값을 변경하기 위함
        private void SetValue(TrackBar control, int value)
        {
            if (control.InvokeRequired)
            {
                Common.SetTBValueCallback d = new Common.SetTBValueCallback(SetValue);
                this.Invoke(d, new object[] { control, value });
            }
            else
            {
                control.Value = value;
            }
        }

        //PictureBox에 그림을 변경하기 위함
        private void SetImage(PictureBox control, Bitmap bitmap)
        {
            if (control.InvokeRequired)
            {
                Common.SetImageCallback d = new Common.SetImageCallback(SetImage);
                this.Invoke(d, new object[] { control, bitmap });
            }
            else
            {
                control.Image = bitmap;
            }
        }

        //ListView 컨트롤의 Item을 삭제하기 위함
        private void SetListViewItemClear(ListView lvControl)
        {
            if (lvControl.InvokeRequired)
            {
                Common.SetListViewItemClearCallback d = new Common.SetListViewItemClearCallback(SetListViewItemClear);
                this.Invoke(d, new object[] { lvControl });
            }
            else
            {
                lvControl.Items.Clear();
            }
        }

        //ListView 컨트롤의 Item을 추가하기 위함
        private void SetListViewItemInster(ListView lvControl, int index, ListViewItem lvItem)
        {
            if (lvControl.InvokeRequired)
            {
                Common.SetListViewItemInsertCallback d = new Common.SetListViewItemInsertCallback(SetListViewItemInster);
                this.Invoke(d, new object[] { lvControl, index, lvItem });
            }
            else
            {
                lvControl.Items.Insert(index, lvItem);
            }
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region AddStatus( 현재상태 DataBase에 저장 )
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        public void AddStatus(string tmpStr)
        {
            try
            {
                Common.StatusListInfo tmpStatus = new Common.StatusListInfo();

                tmpStatus.datetime = DateTime.Now;
                tmpStatus.content = tmpStr;

                fbDBMS_StatusListInfo.Insert(tmpStatus);    //DB에 저장

                //상태창에 표시
                string[] tmpData = new string[4];

                tmpData[0] = lvUseMsg.Items.Count.ToString();
                tmpData[1] = DateTime.Now.ToString("yyyy-MM-dd");
                tmpData[2] = DateTime.Now.ToString("HH:mm:ss");
                tmpData[3] = tmpStr;

                ListViewItem tmpLV = new ListViewItem(tmpData);
                SetListViewItemInster(lvUseMsg, 0, tmpLV);
            }
            catch
            {
            }
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        private void frmMain_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == System.Windows.Forms.MouseButtons.Right)
            {
                if (frmDV == null)
                {
                    frmDV = new Common.frmDataView();
                    frmDV.FormClosing += new FormClosingEventHandler(frmDV_FormClosing);

                    switch (Common.clsNMS.nmsGUIUser)
                    {
                        case "분당선":
                        case "경의일산선":
                        case "수인선":
                            frmDV.SetTitle(0, "  NMS 주장치  ");
                            break;

                        case "과천선":
                            frmDV.SetTitle(0, "  NMS Server Main  ");
                            frmDV.SetTitle(1, "  NMS Server Stby  ");
                            break;
                    }

                    frmDV.Show();
                }
                else
                {
                    MessageBox.Show("이미실행되어 있습니다.", "중복실행확인", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                }
            }
        }

        void frmDV_FormClosing(object sender, FormClosingEventArgs e)
        {
            for (int i = 0; i < 2; i++)
                Common.frmDebug.flagDebug[i] = false;

            frmDV = null;
        }

        private void NMS_Init()
        {
            int i = 0, j = 0;

            StreamReader fr = null;

            for (i = 0; i < Common.clsNMS.stationList.Count; i++)
            {
                try { btMu[i].Tag = i; }
                catch { }

                try { btRuA[i].Tag = i; }
                catch { }

                for (j = 0; j < 4; j++)
                {
                    try { btRuB[i, j].Tag = (i * 10) + j; }
                    catch { }
                }

                try { btMuFm[i].Tag = i; }
                catch { }

                try { btRuAFm[i].Tag = i; }
                catch { }

                for (j = 0; j < 4; j++)
                {
                    try { btRuBFm[i, j].Tag = (i * 10) + j; }
                    catch { }
                }
            }

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                case "분당선":
                case "수인선":
                    break;

                case "경의일산선":
                    Common.clsNMS.muRfValueTable[0] = 0x61;
                    Common.clsNMS.muRfValueTable[1] = 0x66;
                    Common.clsNMS.muRfValueTable[2] = 0x6A;
                    Common.clsNMS.muRfValueTable[3] = 0x6E;
                    Common.clsNMS.muRfValueTable[4] = 0x72;
                    Common.clsNMS.muRfValueTable[5] = 0x76;
                    Common.clsNMS.muRfValueTable[6] = 0x79;
                    Common.clsNMS.muRfValueTable[7] = 0x7D;
                    Common.clsNMS.muRfValueTable[8] = 0x81;
                    Common.clsNMS.muRfValueTable[9] = 0x84;
                    Common.clsNMS.muRfValueTable[10] = 0x88;
                    Common.clsNMS.muRfValueTable[11] = 0x8A;
                    Common.clsNMS.muRfValueTable[12] = 0x8D;
                    Common.clsNMS.muRfValueTable[13] = 0x8D;
                    Common.clsNMS.muRfValueTable[14] = 0x8D;
                    Common.clsNMS.muRfValueTable[15] = 0x8D;

                    Common.clsNMS.ruRfValueTable[0] = 0x76;
                    Common.clsNMS.ruRfValueTable[1] = 0x7F;
                    Common.clsNMS.ruRfValueTable[2] = 0x86;
                    Common.clsNMS.ruRfValueTable[3] = 0x8C;
                    Common.clsNMS.ruRfValueTable[4] = 0x93;
                    Common.clsNMS.ruRfValueTable[5] = 0x99;
                    Common.clsNMS.ruRfValueTable[6] = 0x9F;
                    Common.clsNMS.ruRfValueTable[7] = 0xA5;
                    Common.clsNMS.ruRfValueTable[8] = 0xAA;
                    Common.clsNMS.ruRfValueTable[9] = 0xAE;
                    Common.clsNMS.ruRfValueTable[10] = 0xB4;
                    Common.clsNMS.ruRfValueTable[11] = 0xB9;
                    Common.clsNMS.ruRfValueTable[12] = 0xBC;
                    Common.clsNMS.ruRfValueTable[13] = 0xBC;
                    Common.clsNMS.ruRfValueTable[14] = 0xBC;
                    Common.clsNMS.ruRfValueTable[15] = 0xBC;
                    break;

                default:
                    break;
            }

            /*
            Common.clsNMS.muRfValueTable[0] = 0x61;
            Common.clsNMS.muRfValueTable[1] = 0x66;
            Common.clsNMS.muRfValueTable[2] = 0x6A;
            Common.clsNMS.muRfValueTable[3] = 0x6E;
            Common.clsNMS.muRfValueTable[4] = 0x72;
            Common.clsNMS.muRfValueTable[5] = 0x76;
            Common.clsNMS.muRfValueTable[6] = 0x79;
            Common.clsNMS.muRfValueTable[7] = 0x7D;
            Common.clsNMS.muRfValueTable[8] = 0x81;
            Common.clsNMS.muRfValueTable[9] = 0x84;
            Common.clsNMS.muRfValueTable[10] = 0x88;
            Common.clsNMS.muRfValueTable[11] = 0x8A;
            Common.clsNMS.muRfValueTable[12] = 0x8D;

            Common.clsNMS.ruRfValueTable[0] = 0x76;
            Common.clsNMS.ruRfValueTable[1] = 0x7F;
            Common.clsNMS.ruRfValueTable[2] = 0x86;
            Common.clsNMS.ruRfValueTable[3] = 0x8C;
            Common.clsNMS.ruRfValueTable[4] = 0x93;
            Common.clsNMS.ruRfValueTable[5] = 0x99;
            Common.clsNMS.ruRfValueTable[6] = 0x9F;
            Common.clsNMS.ruRfValueTable[7] = 0xA5;
            Common.clsNMS.ruRfValueTable[8] = 0xAA;
            Common.clsNMS.ruRfValueTable[9] = 0xAE;
            Common.clsNMS.ruRfValueTable[10] = 0xB4;
            Common.clsNMS.ruRfValueTable[11] = 0xB9;
            Common.clsNMS.ruRfValueTable[12] = 0xBC;
            */

            #region 역사별 MU/RU 상태 저장 변수 초기화
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            muruNowData = new Common.MURUData[Common.clsNMS.stationList.Count + 1];

            for (i = 0; i < muruNowData.Length; i++)
                muruNowData[i] = new Common.MURUData();

            //MuRu 이름저장변수 초기화 
            Common.clsNMS.muruName = new Common.MuRuName[Common.clsNMS.stationList.Count + 1];
            for (i = 0; i < muruNowData.Length; i++)
                Common.clsNMS.muruName[i] = new Common.MuRuName();

            //MuRu 통신 상태 저장 변수 초기화
            for (i = 0; i < Common.clsNMS.stationList.Count; i++)
            {
                Common.clsNMS.muruComSt[i] = new Common.MuRuComSt();
                Common.clsNMS.muruFmExist[i] = new Common.MuRuFmExist();
                Common.clsNMS.muLifExist[i] = new Common.MuLifExist();
            }

            for (i = 0; i < 3; i++)
                Common.clsNMS.cceComSt[i] = new Common.CCEComSt();

            commErrCheck = new Common.CommErrCheck[Common.clsNMS.stationList.Count + 1];
            for (i = 0; i < muruNowData.Length; i++)
                commErrCheck[i] = new Common.CommErrCheck();

            //private MURUDataChange[] mrurDataChange = null;
            mrurDataChange = new Common.MURUDataChange[Common.clsNMS.stationList.Count + 1];
            for (i = 0; i < muruNowData.Length; i++)
                mrurDataChange[i] = new Common.MURUDataChange();
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion

            lbMuRuName.Items.Clear();   //MuRu 이름 설정화면의 역사 이름 표시
            cbSetupMU.Items.Add("선택 안함");       //설정화면의 A/S용
            ucGainSet_ILSAN.cbSetupILSANMU.Items.Add("선택 안함");       //설정화면의 A/S용
            for (i = 0; i < Common.clsNMS.stationList.Count; i++)
            {
                lbMuRuName.Items.Add(Common.clsNMS.stationList[i].ToString());

                cbSetupMU.Items.Add(Common.clsNMS.stationList[i].ToString());
                ucGainSet_ILSAN.cbSetupILSANMU.Items.Add(Common.clsNMS.stationList[i].ToString());
            }

            #region 파일로 부터 MU/RU 이름 정보 읽어오기
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                    try
                    {
                        string fileName = @"\\" + Common.clsNMS.nmsServerIP[0] + @"\NMS Server\과천선_Server_MuRuName.ini";

                        try
                        {
                            fr = File.OpenText(fileName);
                        }
                        catch
                        {
                            fileName = @"\\" + Common.clsNMS.nmsServerIP[1] + @"\NMS Server\과천선_Server_MuRuName.ini";

                            try
                            {
                                fr = File.OpenText(fileName);
                            }
                            catch
                            {
                            }
                        }

                        //fr = File.OpenText(Common.clsCommon.DefaultPath + Common.clsNMS.nmsGUIUser + "_MuRuName.ini");

                        int tmpVer = Common.clsCommon.VerCheck_NMSServer(fr.ReadLine());  //Version Read

                        switch (tmpVer)
                        {
                            case 0:     //Ver 1.00 ~
                                for (i = 0; i < muruNowData.Length; i++)
                                {
                                    Common.clsNMS.muruName[i].muName = fr.ReadLine();
                                    for (j = 0; j < 4; j++) Common.clsNMS.muruName[i].ruName[j].ruName = fr.ReadLine();
                                }

                                //Lif사용 유무
                                for (i = 0; i < muruNowData.Length - 1; i++)
                                {
                                    for (j = 0; j < 6; j++)
                                    {
                                        if (fr.ReadLine() == "True")
                                            Common.clsNMS.muLifExist[i].flagLif[j] = true;
                                    }
                                }
                                break;

                            default:
                                //if (clsCommon.flagLanguage) MessageBox.Show("프로그램 버젼이 맞지 않습니다. 확인후 다시 시도하시기 바랍니다.");
                                //else MessageBox.Show("Program version does not match. Try again after verification.");

                                this.Close();
                                break;
                        }
                    }
                    catch
                    {
                    }
                    break;

                case "분당선":
                case "경의일산선":
                case "수인선":
                    try
                    {
                        fr = File.OpenText(Common.clsCommon.DefaultPath + Common.clsNMS.nmsGUIUser + "_MuRuName.ini");

                        int tmpVer = Common.clsCommon.VerCheck_NMS(fr.ReadLine());  //Version Read

                        switch (tmpVer)
                        {
                            case 0:     //Ver 1.00 ~
                                for (i = 0; i < muruNowData.Length; i++)
                                {
                                    Common.clsNMS.muruName[i].muName = fr.ReadLine();
                                    for (j = 0; j < 4; j++) Common.clsNMS.muruName[i].ruName[j].ruName = fr.ReadLine();
                                }

                                //Lif사용 유무
                                for (i = 0; i < muruNowData.Length - 1; i++)
                                {
                                    for (j = 0; j < 6; j++)
                                    {
                                        if (fr.ReadLine() == "True")
                                            Common.clsNMS.muLifExist[i].flagLif[j] = true;
                                    }
                                }
                                break;

                            default:
                                //if (clsCommon.flagLanguage) MessageBox.Show("프로그램 버젼이 맞지 않습니다. 확인후 다시 시도하시기 바랍니다.");
                                //else MessageBox.Show("Program version does not match. Try again after verification.");

                                this.Close();
                                break;
                        }
                    }
                    catch
                    {
                    }
                    break;
            }

            fr.Close();
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion
        }

        #region 공통 메뉴 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void btMenu_Click(object sender, EventArgs e)
        {
            int index = Convert.ToInt16(((Button)sender).Tag);

            switch (index)
            {
                case 0:     //경보해제/복구
                    SetChecked(cbBellPlay, !Common.clsNMS.flagSoundAction);
                    break;

                case 1:     //주화면
                case 2:     //이력검색 화면
                    btMenu_Click(index);
                    break;

                case 3:     //설정화면
                    frmPW_Show(1);
                    break;
            }
        }

        private void btMenu_Click(int index)
        {
            int tmpIndex = index - 1;

            for (int i = 0; i < 3; i++)
            {
                SetColor(btCommMenu[i + 1], SystemColors.Control);
                SetVisible(panelGround[i], false);
            }

            SetColor(btCommMenu[index], Color.YellowGreen);
            SetVisible(panelGround[tmpIndex], true);

            if (tmpIndex == 0)
            {
                SetVisible(panelMainBase[0], true);
                SetVisible(panelMainBase[1], false);
            }

            Common.clsNMS.presentMUID = 0;
            Common.clsNMS.presentRUID = 0;
        }

        #region 비밀번호 입력후 처리
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void frmPW_Show(int tmpPwApplyIndex)
        {
            pwApplyIndex = tmpPwApplyIndex;

            if (frmPW == null)
            {
                frmPW = new Common.frmPassword();
                frmPW.FormClosing += new FormClosingEventHandler(frmPW_FormClosing);
                frmPW.OK_Cancel_Click += new Common.OK_Cancel_Click_Event(frmPW_OK_Cancel_Click);
            }

            frmPW.tbPW_Focus();
            frmPW.Show();
        }

        void frmPW_OK_Cancel_Click(bool flagAction)
        {
            //비밀번호 입력완료시 처리할 내용...
            if (flagAction)
            {
                switch (pwApplyIndex)
                {
                    case 1:     //설정화면으로 넘어가기
                        btMenu_Click(3);
                        break;
                }
            }
        }

        void frmPW_FormClosing(object sender, FormClosingEventArgs e)
        {
            frmPW = null;
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        private void MuStInit(bool flagAction)
        {
            if (flagILSAN) ucMUSt_ILSAN.MuStInit(flagAction);
            else ucMUSt.MuStInit(flagAction);      //MU 화면 초기화
        }

        private void MuFmStInit(bool flagAction)
        {
            if (flagILSAN) ucMUSt_ILSAN.FmStInit(flagAction);
            else ucMUSt.FmStInit(flagAction);      //MU의 FM 화면 초기화
        }

        private void RuStInit(bool flagAction)
        {
            ucRUSt.RuStInit(flagAction);
        }

        private void RuFmStInit(bool flagAction)
        {
            ucRUSt.FmStInit(flagAction);      //RU의 FM 화면 초기화
        }

        #region DB 관련 갱신 기능
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void NMSDB_Connect()
        {
            if (fbDBMS_NMS != null)
            {
                fbDBMS_NMS.Dispose();
                fbDBMS_NMS = null;
            }

            string tmpPath = Common.clsCommon.DataBasePath + DateTime.Now.Year.ToString("0000") + @"\";
            if (!Directory.Exists(tmpPath)) Directory.CreateDirectory(tmpPath);

            string dbFile = "NMS_" + DateTime.Now.Year.ToString("0000") + DateTime.Now.Month.ToString("00") + day.ToString("00") + ".FDB";
            if (!File.Exists(tmpPath + dbFile))
                File.Copy(Common.clsCommon.DataBasePath + "NMS_x64.FDB", tmpPath + dbFile);

            fbDBMS_NMS = new Common.nmsDBMS();
            fbDBMS_NMS.DBConnect(tmpPath + dbFile);
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region 주 화면
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #region MU/RuA/RuB Click 이벤트
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void btMU_Click(object sender, EventArgs e)
        {
            btMU_Click(Convert.ToByte(((Button)sender).Tag));
        }

        private void btRuA_Click(object sender, EventArgs e)
        {
            btRuA_Click(Convert.ToInt32(((Button)sender).Tag));
        }

        private void btRuB_Click(object sender, EventArgs e)
        {
            btRuB_Click(Convert.ToInt32(((Button)sender).Tag));
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
            
        #region MU/RuA/RuB 전체 감시 화면 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void btMU_Click(int index)
        {
            flagILSAN = false;

            nowStation = Common.clsNMS.stationList[index];

            Common.clsNMS.presentMUID = (byte)(index + 1);
            Common.clsNMS.presentRUID = 0;

            SetVisible(panelMainBase[0], false);
            SetVisible(panelMainBase[1], true);

            SetVisible(ucRUSt, false);  //RU 상세화면

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "경의일산선":
                    if (Common.clsNMS.presentMUID > 2) flagILSAN = true;    //일산
                    else ucMUSt.SetMode(1); //경의선 MU 상세화면
                    break;

                case "과천선":
                case "수인선":
                    ucMUSt.SetMode(2);
                    break;

                case "분당선":
                    if (Common.clsNMS.presentMUID > 5) ucMUSt.SetMode(2);
                    else ucMUSt.SetMode(3);
                    break;
            }

            MuStInit(Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].flagMu); //MU 화면 초기화
            if (Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].flagMu)
                nmsMUSt_Display(Common.clsNMS.presentMUID); //MU 상태 적용
            MuFmStInit(Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].flagFm);  //MU FM 초기화
            if (Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].flagFm)
                nmsFMSt_Display(Common.clsNMS.presentMUID); //MU FM 상태 적용

            if (flagILSAN)
            {
                SetVisible(ucMUSt, false);
                SetVisible(ucMUSt_ILSAN, true); //일산선 MU 상세화면

                ucMUSt_ILSAN.SetTitle(nowStation + " 기지국");
            }
            else
            {
                SetVisible(ucMUSt, true);   //MU 상세화면
                SetVisible(ucMUSt_ILSAN, false);

                ucMUSt.SetTitle(nowStation + " 기지국");
            }
        }

        private void btRuA_Click(int index)
        {
            nowStation = Common.clsNMS.stationList[index];

            Common.clsNMS.presentMUID = (byte)(index + 1);
            Common.clsNMS.presentRUID = 0;

            RuStInit(Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagRu); //RU 화면 초기화
            if (Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagRu)
                nmsRUSt_Display(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID); //RU 상태 적용
            RuFmStInit(Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagFm);  //RU FM 초기화
            if (Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagFm)
                nmsFMSt_Display(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID); //RU FM 상태 적용

            SetVisible(panelMainBase[0], false);
            SetVisible(panelMainBase[0], false);
            SetVisible(panelMainBase[1], true);
            SetVisible(ucMUSt, false);  //MU 상세화면
            SetVisible(ucMUSt_ILSAN, false);
            SetVisible(ucRUSt, true);   //RU 상세화면

            ucRUSt.SetMode(0);
            ucRUSt.SetTitle(nowStation + " 광중계장치 ( A 형 )");
        }

        private void btRuB_Click(int index)
        {
            nowStation = Common.clsNMS.muruName[index / 10].ruName[index % 10].ruName;

            Common.clsNMS.presentMUID = (byte)(index / 10 + 1);
            Common.clsNMS.presentRUID = (byte)(index % 10 + 1);

            RuStInit(Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagRu); //RU 화면 초기화
            if (Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagRu)
                nmsRUSt_Display(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID); //RU 상태 적용
            RuFmStInit(Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagFm);  //RU FM 초기화
            if (Common.clsNMS.muruComSt[Common.clsNMS.presentMUID - 1].ruBdaCommSt[Common.clsNMS.presentRUID].flagFm)
                nmsFMSt_Display(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID); //RU FM 상태 적용

            SetVisible(panelMainBase[0], false);
            SetVisible(panelMainBase[1], true);
            SetVisible(ucMUSt, false);  //MU 상세화면
            SetVisible(ucMUSt_ILSAN, false);
            SetVisible(ucRUSt, true);   //RU 상세화면

            ucRUSt.SetMode(1);
            ucRUSt.SetTitle(nowStation + " 광중계장치 ( B 형 )");
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region MU 감시화면 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        void MUSt_muControlClick(byte btCode, byte index, byte value)
        {
            byte[] sendBuf = null;
            string tmpStr = string.Empty;

            Common.clsNMS.flagMuRuA = true;

            switch (btCode)
            {
                case 0:     //Main/Stby 절체
                    if (muruNowData[Common.clsNMS.presentMUID].muData.otherSt1[0] == 0)
                    {
                        MessageBox.Show("절체방법이 자동으로 되어있습니다. 절체방법이 수동으로 되어 있을때만 주/예비 절체를 할수있습니다.");
                        return;
                    }

                    if (MessageBox.Show("주/예비 절체시 통화가 끊어질수 있습니다. 절체 하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    sendBuf = nmsSendDataMake.EditData_CTL_MainStbyChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(index));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 기지국";

                    if (index == 0) tmpStr += " : MAIN으로";
                    else tmpStr += " : STBY로";

                    AddStatus(tmpStr + "장비로 절체하였습니다.");
                    break;

                case 1:     //Auto/Manual 절체
                    if (MessageBox.Show("자동/수동 절체시 통화가 끊어질수 있습니다. 절체 하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    sendBuf = nmsSendDataMake.EditData_CTL_AutoManualChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(index));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    
                    tmpStr = nowStation + " 기지국 : 절체방법을 ";

                    if (index == 0) tmpStr += "AUTO로";
                    else tmpStr += "MANUAL로";

                    AddStatus(tmpStr + " 변경하였습니다.");
                    break;

                case 2:     //Rf 출력값 설정(Main/Stby)
                    if (MessageBox.Show("송신 출력값을 설정하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    byte rfValue = value;

                    try
                    {
                        if ((rfValue < 5) || (rfValue > 20))
                        {
                            MessageBox.Show("설정 범위를 벋어났습니다. 설정범위는 5W ~ 20W 입니다.");
                            return;
                        }

                        rfValue = Common.clsNMS.muRfValueTable[rfValue - 5];
                    }
                    catch
                    {
                        rfValue = 0;
                    }

                    sendBuf = nmsSendDataMake.EditData_CTL_RFValueSet(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(index), rfValue);
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 기지국";

                    if (index == 0) tmpStr += " : 주장비";
                    else tmpStr += " : 예비장비";

                    AddStatus(tmpStr + " 송신 출력을 " + value.ToString() + "W로 설정하였습니다.");
                    break;

                case 3:     //무전기 형식 설정(Wide/Narrow)
                    if (MessageBox.Show("무전기 형식을 변경하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    byte wideNarrow = 0;

                    if (muruNowData[Common.clsNMS.presentMUID].muData.otherSt1[2] == 0) wideNarrow = 1;
                    else wideNarrow = 0;

                    sendBuf = nmsSendDataMake.EditData_CTL_WideNarrowChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(wideNarrow));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 기지국 : 무전기 형식을";

                    if (wideNarrow == 0) tmpStr += " Wide";
                    else tmpStr += " Narrow";

                    AddStatus(tmpStr + "로 변경하였습니다.");
                    break;

                case 4:     //채널복귀시간 설정
                    if (MessageBox.Show("체널 복귀 시간을 설정하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    sendBuf = nmsSendDataMake.EditData_CTL_ChReturnTimeSet(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, value);
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 기지국 :";

                    tmpStr += " 채널복귀시간을 " + value.ToString();

                    AddStatus(tmpStr + "초로 설정하였습니다.");
                    break;

                case 5:     //송신출력시험
                    tmpStr = nowStation + " 기지국 : 송신출력시험을";

                    if (ucMUSt.flagSendOutTest)
                    {   //시험 중지
                        ucMUSt.flagSendOutTest = false;
                        tmpStr += " 중지하였습니다.";
                    }
                    else
                    {   //시험 시작
                        ucMUSt.flagSendOutTest = true;
                        tmpStr += " 시작하였습니다.";
                    }

                    sendBuf = nmsSendDataMake.EditData_CTL_SendOutTest(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, ucMUSt.flagSendOutTest);
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    ucMUSt.btMuSendOutTest_ColorSet();    //송신출력시험 버튼 색 변경

                    AddStatus(tmpStr);
                    break;
            }
        }

        private void ucMUSt_ILSAN_muControlClick(byte btCode, byte index, byte value)
        {
            byte[] sendBuf = null;
            string tmpStr = string.Empty;
            
            switch (btCode)
            {
                case 0:     //CPU 절체(Main/Stby 절체)
                    if (MessageBox.Show("CPU 절체시 통화가 끊어질수 있습니다. 절체 하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes)
                        return;

                    sendBuf = nmsSendDataMake.EditData_ILSAN_CTL_CPUChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(index));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 기지국";

                    if (index == 0) tmpStr += " : CPU1(MAIN)으로";
                    else tmpStr += " : CPU2(STBY)로";

                    AddStatus(tmpStr + " 절체하였습니다.");
                    break;
            }
        }

        void MuStatusReport(int myID, byte muID, Common.MUData muData)
        {
            int i = 0;

            if (!Common.clsNMS.muruComSt[muID - 1].flagMuFirst)
                Common.clsNMS.muruComSt[muID - 1].flagMuFirst = true;

            Common.clsNMS.muruComSt[muID - 1].cntMu = 0;
            if (!Common.clsNMS.muruComSt[muID - 1].flagMu)
            {
                AddStatus(Common.clsNMS.stationList[muID - 1] + "(MU) 통신이상 복구");

                Common.clsNMS.muruComSt[muID - 1].flagMu = true;

                //현재 MU화면을 보고 있으면 활성화 시킴.(통신 불량에서 정상으로 돌아온경우..)
                if (muID == Common.clsNMS.presentMUID) MuStInit(true);

                SetColor(btMu[muID - 1], colorSelect);
                SetForeColor(btMu[muID - 1], Color.Black);
            }

            //PTT가 잡히지 않으면 출력값은 0이다.
            if (muData.otherSt2[2] == 0) muData.rfValue[0] = 0;
            if (muData.otherSt2[4] == 0) muData.rfValue[1] = 0;

            //주/예비/감청 통신 Error Check
            for (i = 0; i < 3; i++)
            {
                if (muData.optMonSt[i] == 0)    //정상
                {
                    commErrCheck[muID].cntCommErr[i] = 0;
                    commErrCheck[muID].flagCommErr[i] = false;
                }
                else    //이상
                {
                    if (commErrCheck[muID].cntCommErr[i] < 60)
                        muData.optMonSt[i] = 0;     //일단 정상처리(1분간 유지되면 이상 처리)
                    else
                        commErrCheck[muID].cntCommErr[i] = 61;

                    commErrCheck[muID].flagCommErr[i] = true;
                }
            }

            //LIF Error Check
            for (i = 0; i < 6; i++)
            {   //LIF 사용안하면 정상처리
                if (!Common.clsNMS.muLifExist[muID - 1].flagLif[i])
                    muData.lifSt[i] = 0;
            }

            muruNowData[muID].muData = muData.Clone();

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    nmsMUSt_DBSave(muID);   //DB에 저장
                    break;
            }

            //현재 보고있는 MU화면의 정보를 화면에 표시한다.
            if (muID == Common.clsNMS.presentMUID) nmsMUSt_Display(muID);     //MU 상태 적용

            nmsMUError_Display(muID);   //전체화면의 stationSt 컨트롤에 주예비 상태 및 장애 표시
        }

        void ILSAN_MuStatusReport(int myID, byte muID, Common.ILSAN_MUData muData)
        {
            if (!Common.clsNMS.muruComSt[muID - 1].flagMuFirst)
                Common.clsNMS.muruComSt[muID - 1].flagMuFirst = true;

            Common.clsNMS.muruComSt[muID - 1].cntMu = 0;
            if (!Common.clsNMS.muruComSt[muID - 1].flagMu)
            {
                AddStatus(Common.clsNMS.stationList[muID - 1] + "(MU) 통신이상 복구");

                Common.clsNMS.muruComSt[muID - 1].flagMu = true;

                //현재 MU화면을 보고 있으면 활성화 시킴.(통신 불량에서 정상으로 돌아온경우..)
                if (muID == Common.clsNMS.presentMUID) MuStInit(true);

                SetColor(btMu[muID - 1], colorSelect);
                SetForeColor(btMu[muID - 1], Color.Black);
            }            

            muruNowData[muID].muData_ILSAN = muData.Clone();

            nmsMUSt_ILSAN_DBSave(muID);   //DB에 저장

            //현재 보고있는 MU화면의 정보를 화면에 표시한다.
            if (muID == Common.clsNMS.presentMUID) nmsMUSt_Display(muID);     //MU 상태 적용

            ucGainSet_ILSAN.AfGainReport(0, muID, muruNowData[muID].muData_ILSAN.muGain);   //게인값 표시

            nmsMUError_ILSAN_Display(muID);     //전체화면의 stationSt 컨트롤에 주예비 상태 및 장애 표시
        }

        void MuFmStReport(int myID, byte muID, byte ruID, Common.FM_Info tmpFmInfo)
        {
            //if (!Common.clsNMS.muruFmExist[muID - 1].flagMu) return;

            if (!Common.clsNMS.muruComSt[muID - 1].flagFmFirst)
                Common.clsNMS.muruComSt[muID - 1].flagFmFirst = true;

            Common.clsNMS.muruComSt[muID - 1].cntFm = 0;
            if (!Common.clsNMS.muruComSt[muID - 1].flagFm)
            {
                Common.clsNMS.muruComSt[muID - 1].flagFm = true;

                //현재 RU화면을 보고 있으면 활성화 시킴.(통신 불량에서 정상으로 돌아온경우..)
                if (muID == Common.clsNMS.presentMUID) MuFmStInit(true);

                AddStatus(Common.clsNMS.stationList[muID - 1] + "(MU FM) 통신이상 복구");
            }

            muruNowData[muID].fmInfo = tmpFmInfo.Clone();

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    nmsMuFmSt_DBSave(muID);   //DB에 저장
                    break;
            }

            //현재 보고있는 화면의 정보가 변경되면 화면에 표시한다.
            if ((muID == Common.clsNMS.presentMUID) && (ruID == Common.clsNMS.presentRUID)) nmsFMSt_Display(muID);

            //nmsMUError_Display(muID); //전체화면에 MU 및 FM 장애 표시
            nmsMuFmError_Display(muID); //전체화면레 MU FM 장애표시 
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region RU 감시화면 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void RuSt_ruControlClick(byte btCode, byte index, byte value)
        {
            byte[] sendBuf = null;
            string tmpStr = string.Empty;

            Common.clsNMS.flagMuRuA = false;

            switch (btCode)
            {
                case 0:     //주/예비 절체
                    if (muruNowData[Common.clsNMS.presentMUID].ruData[Common.clsNMS.presentRUID].ruData.otherSt1[0] == 0)
                    {
                        MessageBox.Show("절체방법이 자동으로 되어있습니다. 수동으로 되어 있을때만 주/예비 절체를 할수있습니다.");
                        return;
                    }

                    if (MessageBox.Show("주/예비 절체시 통화가 끊어질수 있습니다. 절체 하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    sendBuf = nmsSendDataMake.EditData_CTL_MainStbyChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(index));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 광중계장치";

                    if (index == 0) tmpStr += " : 주";
                    else tmpStr += " : 예비";

                    AddStatus(tmpStr + "장비로 절체하였습니다.");
                    break;

                case 1:     //자동/수동 절체방법 변경
                    if (MessageBox.Show("자동/수동 절체시 통화가 끊어질수 있습니다. 절체 하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    byte autoManual = 0;

                    if (muruNowData[Common.clsNMS.presentMUID].ruData[Common.clsNMS.presentRUID].ruData.otherSt1[0] == 0) autoManual = 1;
                    else autoManual = 0;

                    sendBuf = nmsSendDataMake.EditData_CTL_AutoManualChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(autoManual));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 광중계장치 : 절체방법을";

                    if (autoManual == 0) tmpStr += " 자동으로";
                    else tmpStr += " 수동으로";

                    AddStatus(tmpStr + " 변경하였습니다.");
                    break;

                case 2:     //RF 출력 조절
                    if (MessageBox.Show("송신 출력값을 설정하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    byte mainSpare = 0;
                    byte rfValue = 0;

                    try
                    {
                        rfValue = value;

                        if ((rfValue < 5) || (rfValue > 20))
                        {
                            MessageBox.Show("설정 범위를 벋어났습니다. 설정범위는 5W ~ 20W 입니다.");
                            return;
                        }

                        rfValue = Common.clsNMS.ruRfValueTable[rfValue - 5];
                    }
                    catch
                    {
                        rfValue = 0;
                    }

                    sendBuf = nmsSendDataMake.EditData_CTL_RFValueSet(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(mainSpare), rfValue);
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 광중계장치 : ";

                    AddStatus(tmpStr + "송신 출력을 " + value.ToString() + "W로 설정하였습니다.");
                    break;

                case 3:     //Wide/Narrow 절체
                    if (MessageBox.Show("무전기 형식을 변경하시겠습니까?",
                        "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes) return;

                    byte wideNarrow = 0;

                    if (muruNowData[Common.clsNMS.presentMUID].ruData[Common.clsNMS.presentRUID].ruData.otherSt1[2] == 0) wideNarrow = 1;
                    else wideNarrow = 0;

                    sendBuf = nmsSendDataMake.EditData_CTL_WideNarrowChange(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, Common.clsCommon.HexaConvert(wideNarrow));
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    tmpStr = nowStation + " 광중계장치 : 무전기형식을";

                    if (wideNarrow == 0) tmpStr += " Wide";
                    else tmpStr += " Narrow";

                    AddStatus(tmpStr + "로 변경하였습니다.");
                    break;

                case 4:
                    tmpStr = nowStation + " 광중계장치 : 송신출력시험을";

                    if (ucRUSt.flagSendOutTest)
                    {   //시험 중지
                        ucRUSt.flagSendOutTest = false;
                        tmpStr += " 중지하였습니다.";
                    }
                    else
                    {   //시험 시작
                        ucRUSt.flagSendOutTest = true;
                        tmpStr += " 시작하였습니다.";
                    }

                    sendBuf = nmsSendDataMake.EditData_CTL_SendOutTest(Common.clsNMS.presentMUID, Common.clsNMS.presentRUID, ucRUSt.flagSendOutTest);
                    JksSockMain.ClientSendData(sendBuf, (byte)sendBuf.Length);
                    JksSockStby.ClientSendData(sendBuf, (byte)sendBuf.Length);

                    ucRUSt.btRuSendOutTest_ColorSet();    //송신출력시험 버튼 색 변경

                    AddStatus(tmpStr);
                    break;
            }
        }

        void RuStatusReport(int myID, byte muID, byte ruID, Common.RUDetailData ruData)
        {
            Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagRuFirst = true;

            Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].cntRu = 0;
            if (!Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagRu)
            {
                if (ruID == 0) AddStatus(Common.clsNMS.muruName[muID - 1].muName + "(RU_A) 통신이상 복구");
                else
                {
                    if (Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName != "")
                        AddStatus(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + "(RU_B) 통신이상 복구");
                }

                Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagRu = true;

                //현재 RU화면을 보고 있으면 활성화 시킴.(통신 불량에서 정상으로 돌아온경우..)
                if ((muID == Common.clsNMS.presentMUID) && (ruID == Common.clsNMS.presentRUID)) RuStInit(true);
            }

            muruNowData[muID].ruData[ruID].ruData = ruData.Clone();

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    nmsRUSt_DBSave(muID, ruID);     //DB에 저장
                    break;
            }

            //현재 보고있는 RU화면의 정보를 화면에 표시한다.
            if ((muID == Common.clsNMS.presentMUID) && (ruID == Common.clsNMS.presentRUID)) nmsRUSt_Display(muID, ruID);    //RU 상태 적용

            nmsRUError_Display(muID, ruID);  //RuSt 컨트롤에 주예비 상태 및 장애 표시
        }

        void RuFmStReport(int myID, byte muID, byte ruID, Common.FM_Info tmpFmInfo)
        {
            if (ruID == 0)
            {
                //if (!Common.clsNMS.muruFmExist[muID - 1].flagMu) return;

                if (!Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFmFirst)
                    Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFmFirst = true;

                Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].cntFm = 0;
                if (!Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFm)
                {
                    Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFm = true;

                    //현재 RU화면을 보고 있으면 활성화 시킴.(통신 불량에서 정상으로 돌아온경우..)
                    if ((muID == Common.clsNMS.presentMUID) && (ruID == Common.clsNMS.presentRUID)) RuFmStInit(true);

                    AddStatus(Common.clsNMS.stationList[muID - 1] + "(RU_A FM) 통신이상 복구");
                }
            }
            else
            {
                //if (!Common.clsNMS.muruFmExist[muID - 1].flagRu[ruID - 1]) return;

                if (!Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFmFirst)
                    Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFmFirst = true;

                Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].cntFm = 0;
                if (!Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFm)
                {
                    Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFm = true;

                    //현재 RU화면을 보고 있으면 활성화 시킴.(통신 불량에서 정상으로 돌아온경우..)
                    if ((muID == Common.clsNMS.presentMUID) && (ruID == Common.clsNMS.presentRUID)) RuFmStInit(true);

                    string tmpStr = Common.clsNMS.stationList[muID - 1] + " - " + Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName;
                    AddStatus(tmpStr + "(RU_B FM) 통신이상 복구");
                }
            }

            muruNowData[muID].ruData[ruID].fmInfo = tmpFmInfo.Clone();

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    nmsRuFmSt_DBSave(muID, ruID);     //DB에 저장
                    break;
            }

            //현재 보고있는 화면의 정보가 변경되면 화면에 표시한다.
            if ((muID == Common.clsNMS.presentMUID) && (ruID == Common.clsNMS.presentRUID)) nmsFMSt_Display(muID, ruID);

            //nmsRUError_Display(muID, ruID);   //전체화면에 RU 및 FM 장애 표시
            nmsRuFmError_Display(muID, ruID);   //전체화면레 RU FM 장애표시 
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region 설정 화면
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #region MU/RU 이름 설정 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private int lbMuRuNameSelectedIndex = 0;

        private void tbMuRuName_TextChanged(object sender, EventArgs e)
        {
            int index = Convert.ToInt32(((TextBox)sender).Tag);

            if (index == 0)  //MU Name
                Common.clsNMS.muruName[lbMuRuNameSelectedIndex].muName = ((TextBox)sender).Text;
            else            //RU Name(1~4)
                Common.clsNMS.muruName[lbMuRuNameSelectedIndex].ruName[index - 1].ruName = ((TextBox)sender).Text;
        }

        private void chkLif_CheckedChanged(object sender, EventArgs e)
        {
            int index = Convert.ToInt32(((CheckBox)sender).Tag);
            Common.clsNMS.muLifExist[lbMuRuNameSelectedIndex].flagLif[index] = chkLif[index].Checked;
        }

        private void lbMuRuName_SelectedIndexChanged(object sender, EventArgs e)
        {
            int i = 0;

            lbMuRuNameSelectedIndex = lbMuRuName.SelectedIndex;

            SetText(tbMuName, Common.clsNMS.muruName[lbMuRuNameSelectedIndex].muName);

            for (i = 0; i < 4; i++)
            {
                SetText(tbRuName[i], Common.clsNMS.muruName[lbMuRuNameSelectedIndex].ruName[i].ruName);
            }

            try
            {
                for (i = 0; i < 6; i++)
                    SetChecked(chkLif[i], Common.clsNMS.muLifExist[lbMuRuNameSelectedIndex].flagLif[i]);
            }
            catch
            {
            }
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region 기타 설정 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void btOpenDialog_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            if (openFileDialog.ShowDialog() == DialogResult.OK)
                SetText(tbBellFile, openFileDialog.FileName);
        }

        private void cbBellPlay_CheckedChanged(object sender, EventArgs e)
        {
            Common.clsNMS.flagSoundAction = cbBellPlay.Checked;

            if (Common.clsNMS.flagSoundAction) SetColor(btMenu1, colorBase);        //경보
            else SetColor(btMenu1, colorSelect);                //경보해제            
        }

        private void btBellPlay_Click(object sender, EventArgs e)
        {
            BellSoundPlay(tbBellFile.Text);
        }

        private void btSetupSave_Click(object sender, EventArgs e)
        {
            int i = 0, j = 0;
            
            StreamWriter fw = null;

            if (sender.Equals(btSetupSave1))
            {
                #region 기타설정
                //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                if (MessageBox.Show("이 설정을 저장(변경)하면 시스템에 심각한 장애가 발생할수 있습니다. 그래도 설정값을 변경 하시겠습니까?",
                    "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes)
                    return;

                try
                {
                    Common.clsNMS.nmsGUIUser = cbUser.Text;   //사용처                 

                    //NMS 정보
                    Common.clsNMS.nmsServerIP[0] = tbNMSIP1.Text;
                    Common.clsNMS.nmsServerIP[1] = tbNMSIP2.Text;
                    Common.clsNMS.nmsPort = Convert.ToInt16(tbNMSPort.Text);

                    //경보 관련 정보
                    Common.clsNMS.flagSoundAction = cbBellPlay.Checked;     //경보음 재생 유무
                    Common.clsNMS.bellPlayCycle = Convert.ToInt32(tbBellPlayCycle.Text);  //경보음 재생 주기
                    Common.clsNMS.bellFile = tbBellFile.Text;    //경보음 파일

                    //DB 정보
                    Common.clsCommon.ServerPath = tbServerPath.Text;
                }
                catch
                {
                }

                fw = File.CreateText(Common.clsCommon.DefaultPath + "setup.txt");

                try
                {
                    fw.WriteLine(Common.clsCommon.thisVer);     //버젼 저장

                    fw.WriteLine(Common.clsNMS.nmsGUIUser);   //사용처

                    //NMS Server 정보
                    fw.WriteLine(Common.clsNMS.nmsServerIP[0]);       //NMS Server Main IP
                    fw.WriteLine(Common.clsNMS.nmsServerIP[1]);       //NMS Server Stby IP
                    fw.WriteLine(Common.clsNMS.nmsPort.ToString());   //카메라 Port

                    //경보 관련 정보
                    fw.WriteLine(Common.clsNMS.flagSoundAction);                    //경보음 재생 유무
                    fw.WriteLine(Common.clsNMS.bellPlayCycle.ToString());     //경보음 재생 주기
                    fw.WriteLine(Common.clsNMS.bellFile);                     //경보음 파일

                    //DB 정보
                    fw.WriteLine(Common.clsCommon.ServerPath);

                    AddStatus("기타 설정을 저장하였습니다.");
                }
                catch
                {
                    AddStatus("기타 설정을 저장하는데 문제가 발생하였습니다. 확인후 다시 시도하시기 바랍니다.");
                }

                fw.Close();

                SetApply();     //저장된 정보 적용

                MessageBox.Show("설정이 변경되었습니다. 프로그램을 다시 실행해 주시기 바랍니다.", "경고", MessageBoxButtons.OK);
                //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                #endregion
            }
            else
            {
                #region MU/RU 이름 설정
                //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                if (MessageBox.Show("RU이름 및 역사이름을 저장하시겠습니까?",
                    "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes)
                    return;

                fw = File.CreateText(Common.clsCommon.DefaultPath + Common.clsNMS.nmsGUIUser + "_MuRuName.ini");

                try
                {
                    fw.WriteLine(Common.clsCommon.thisVer);      //버젼 저장

                    for (i = 0; i < muruNowData.Length; i++)
                    {
                        fw.WriteLine(Common.clsNMS.muruName[i].muName);

                        for (j = 0; j < 4; j++) fw.WriteLine(Common.clsNMS.muruName[i].ruName[j].ruName);
                    }

                    //Lif사용 유무
                    for (i = 0; i < muruNowData.Length - 1; i++)
                    {
                        for (j = 0; j < 6; j++)
                            fw.WriteLine(Common.clsNMS.muLifExist[i].flagLif[j]);
                    }

                    AddStatus("MU/RU 이름을 저장하였습니다.");
                }
                catch
                {
                    AddStatus("MU/RU 이름을 저장하는데 문제가 발생하였습니다. 확인후 다시 시도하시기 바랍니다.");
                }

                fw.Close();
                //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                #endregion
            }
        }

        private void cbUser_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cbUser.Text)
            {
                case "과천선":
                case "과천선_Server":
                    SetVisible(panelSetupName, false);
                    panelSetupOther.Left = 253;

                    SetText(gbSetupNMSInfo, "NMS Server 정보");
                    SetText(lblNMSIP1, "MAIN IP");
                    lblNMSIP1.Top = 31;
                    tbNMSIP1.Top = 26;

                    SetVisible(lblNMSIP2, true);
                    SetVisible(tbNMSIP2, true);

                    lblNMSPort.Top = 87;
                    tbNMSPort.Top = 82;

                    SetVisible(gbServerPath, true);

                    gbSetupUser.Top = 85;
                    gbSetupNMSInfo.Top = 85;
                    gbSelectAlarm.Top = 281;
                    btSetupSave1.Height = 286;
                    btSetupSave1.Top = 85;
                    break;

                case "분당선":
                case "수인선":
                default:
                    SetVisible(panelSetupName, true);
                    panelSetupOther.Left = 480;

                    SetText(gbSetupNMSInfo, "NMS 주장치 정보");
                    SetText(lblNMSIP1, "주장치 IP");
                    lblNMSIP1.Top = 45;
                    tbNMSIP1.Top = 40;

                    SetVisible(lblNMSIP2, false);
                    SetVisible(tbNMSIP2, false);

                    lblNMSPort.Top = 73;
                    tbNMSPort.Top = 68;

                    SetVisible(gbServerPath, false);

                    gbSetupUser.Top = 116;
                    gbSetupNMSInfo.Top = 116;
                    gbSelectAlarm.Top = 238;
                    btSetupSave1.Height = 217;
                    btSetupSave1.Top = 111;
                    break;
            }
        }

        #region 설정 정보 적용
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void SetInput()
        {
            SetText(cbUser, Common.clsNMS.nmsGUIUser);    //사용처

            //NMS 주장치 정보
            SetText(tbNMSIP1, Common.clsNMS.nmsServerIP[0]);
            SetText(tbNMSIP2, Common.clsNMS.nmsServerIP[1]);
            SetText(tbNMSPort, Common.clsNMS.nmsPort.ToString());

            SetChecked(cbBellPlay, Common.clsNMS.flagSoundAction);                  //경보 재생 유무
            SetText(tbBellPlayCycle, Common.clsNMS.bellPlayCycle.ToString()); //경보재생주기
            SetText(tbBellFile, Common.clsNMS.bellFile);                      //경보음 파일
            
            if (Common.clsNMS.flagSoundAction) SetColor(btMenu1, colorBase);    //경보
            else SetColor(btMenu1, colorSelect);            //경보해제

            SetText(tbServerPath, Common.clsCommon.ServerPath);   //Database Path 정보

            SetApply();
        }

        private void SetApply()
        {
            JksSockMain.ClientInit(Common.clsNMS.nmsServerIP[0], Common.clsNMS.nmsPort);
            JksSockStby.ClientInit(Common.clsNMS.nmsServerIP[1], Common.clsNMS.nmsPort);

            tmrBell.Stop();
            tmrBell.Interval = Common.clsNMS.bellPlayCycle;
            tmrBell.Start();
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
        
        #region A/S용 설정 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void cbSetupMU_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "경의일산선":
                    ucGainSet_ILSAN.afGainSelectedIndex = cbSetupMU.SelectedIndex;

                    if (cbSetupMU.SelectedIndex == 0) return;

                    if (cbSetupMU.SelectedIndex > 2)
                    {
                        SetVisible(gbMuAFGain, false);
                        SetVisible(gbILSAN_MuAFGain, true);

                        ucGainSet_ILSAN.AfGainReport(0, (byte)(cbSetupMU.SelectedIndex - 1), muruNowData[cbSetupMU.SelectedIndex - 1].muData_ILSAN.muGain);
                    }
                    else
                    {
                        SetVisible(gbMuAFGain, true);
                        SetVisible(gbILSAN_MuAFGain, false);
                    }

                    ucGainSet_ILSAN.cbSetupILSANMU.Text = cbSetupMU.Text;
                    break;
            }
        }

        private void btAfGainInquiry_Click(object sender, EventArgs e)
        {
            if (btAfGainInquiry.BackColor != colorBase)
            {
                SetColor(btAfGainInquiry, colorBase);
                return;
            }

            if (cbSetupMU.SelectedIndex < 1)
            {
                MessageBox.Show("AF Gain값을 조회할 MU(역사)를 선택해주시기 바랍니다.");
                return;
            }

            Common.clsNMS.flagMuRuA = true;

            byte tmpMuID = (byte)cbSetupMU.SelectedIndex;

            SetColor(btAfGainInquiry, Color.Yellow);

            byte[] tmpBuffer = nmsSendDataMake.EditData_AfGainInquiry(tmpMuID);
            JksSockMain.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);
            JksSockStby.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);

            string tmpStr = Common.clsNMS.stationList[tmpMuID - 1] + "의 MU";

            AddStatus(tmpStr + " : AFGain값을 요청하였습니다.");
        }

        private void btAfGainSet_Click(object sender, EventArgs e)
        {   //AF Gain값 설정
            if (cbSetupMU.SelectedIndex < 1)
            {
                MessageBox.Show("AF Gain값을 설정할 MU(역사)를 선택해주시기 바랍니다.");
                return;
            }

            if (MessageBox.Show("AFGain 값을 설정하시겠습니까?",
                "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes)
                return;

            Common.clsNMS.flagMuRuA = true;

            int index = Convert.ToInt32(((Button)sender).Tag);
            byte muIndex = (byte)cbSetupMU.SelectedIndex;

            List<TextBox> tmpAfGain = new List<TextBox>();
            switch (index)
            {
                case 0: tmpAfGain = tbCCEAfGain; break;
                case 1: tmpAfGain = tbCCE2AfGain; break;
                case 2: tmpAfGain = tbRC1AfGain; break;
                case 3: tmpAfGain = tbRC2AfGain; break;
                case 4: tmpAfGain = tbRC3AfGain; break;
                case 5: tmpAfGain = tbRC4AfGain; break;
            }

            int[] afGainValue = new int[3];

            for (int i = 0; i < 3; i++)
            {
                try
                {
                    afGainValue[i] = Convert.ToInt32(tmpAfGain[i].Text);

                    if((afGainValue[i] < -10) || (afGainValue[i] > 10))
                    {
                        MessageBox.Show("설정값의 범위를 벗어났습니다. 설정값의 범위는 -10 ~ 10 입니다.");
                        return;
                    }
                }
                catch
                {
                    //afGainValue[i] = 0;
                    MessageBox.Show("설정값의 범위를 벗어났습니다. 설정값의 범위는 -10 ~ 10 입니다.");
                    return;
                }
            }

            byte[] tmpBuffer = nmsSendDataMake.EditData_CTL_AFValueSet(muIndex, 0, (byte)index, afGainValue);
            JksSockMain.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);
            JksSockStby.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);

            string tmpStr = Common.clsNMS.stationList[muIndex - 1] + "의 MU";

            switch (index)
            {
                case 0: tmpStr += " : CCE1"; break;
                case 1: tmpStr += " : CCE2"; break;
                case 2: tmpStr += " : RC1"; break;
                case 3: tmpStr += " : RC2"; break;
                case 4: tmpStr += " : RC3"; break;
                case 5: tmpStr += " : RC4"; break;
            }

            tmpStr += "의 Gain 값을 (";

            tmpStr += "TX : " + afGainValue[0].ToString() + ", ";
            tmpStr += "RX : " + afGainValue[1].ToString() + ", ";
            tmpStr += "Mon : " + afGainValue[2].ToString();

            AddStatus(tmpStr + " ) 설정하였습니다.");
        }

        private void btAfGainSetRepeat_Click(object sender, EventArgs e)
        {
            if (cbSetupMU.SelectedIndex < 1)
            {
                MessageBox.Show("AF Gain값을 설정할 MU(역사)를 선택해주시기 바랍니다.");
                return;
            }

            if (MessageBox.Show("REPEAT의 AFGain 값을 설정하시겠습니까?",
                "경고", MessageBoxButtons.YesNo, MessageBoxIcon.Stop) != DialogResult.Yes)
                return;

            Common.clsNMS.flagMuRuA = true;

            byte muIndex = (byte)cbSetupMU.SelectedIndex;

            int afGainValue = new int();
            try
            {
                afGainValue = Convert.ToInt32(tbRepeatAfGain.Text);

                if ((afGainValue < -10) || (afGainValue > -10))
                {
                    MessageBox.Show("설정값의 범위를 벗어났습니다. 설정값의 범위는 -10 ~ 10 입니다.");
                    return;
                }
            }
            catch
            {
                //afGainValue = 0;
                MessageBox.Show("설정값의 범위를 벗어났습니다. 설정값의 범위는 -10 ~ 10 입니다.");
                return;
            }

            byte[] tmpBuffer = nmsSendDataMake.EditData_CTL_AFValueSetRepeat(muIndex, 0, afGainValue);
            JksSockMain.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);
            JksSockStby.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);


            string tmpStr = Common.clsNMS.stationList[muIndex - 1] + "의 MU REPEAT의 Gain 값을 (" + afGainValue.ToString();
            AddStatus(tmpStr + " ) 설정하였습니다.");
        }

        void AfGainReport(int myID, byte muID, Common.AFGain[] afGain)
        {
            for (int i = 0; i < 6; i++)
            {
                if (afGain[i].gainTx >= 0x80) afGain[i].gainTx = (afGain[i].gainTx - 0x80) * -1;
                if (afGain[i].gainRx >= 0x80) afGain[i].gainRx = (afGain[i].gainRx - 0x80) * -1;
                if (afGain[i].gainMon >= 0x80) afGain[i].gainMon = (afGain[i].gainMon - 0x80) * -1;
            }
            if (afGain[6].gainTx >= 0x80) afGain[6].gainTx = (afGain[6].gainTx - 0x80) * -1;

            SetText(lblCCEAfGain1, afGain[0].gainTx.ToString() + " dB");
            SetText(lblCCEAfGain2, afGain[0].gainRx.ToString() + " dB");
            SetText(lblCCEAfGain3, afGain[0].gainMon.ToString() + " dB");

            SetText(lblRC1AfGain1, afGain[1].gainTx.ToString() + " dB");
            SetText(lblRC1AfGain2, afGain[1].gainRx.ToString() + " dB");
            SetText(lblRC1AfGain3, afGain[1].gainMon.ToString() + " dB");

            SetText(lblRC2AfGain1, afGain[2].gainTx.ToString() + " dB");
            SetText(lblRC2AfGain2, afGain[2].gainRx.ToString() + " dB");
            SetText(lblRC2AfGain3, afGain[2].gainMon.ToString() + " dB");

            SetText(lblRC3AfGain1, afGain[3].gainTx.ToString() + " dB");
            SetText(lblRC3AfGain2, afGain[3].gainRx.ToString() + " dB");
            SetText(lblRC3AfGain3, afGain[3].gainMon.ToString() + " dB");

            SetText(lblCCE2AfGain1, afGain[4].gainTx.ToString() + " dB");
            SetText(lblCCE2AfGain2, afGain[4].gainRx.ToString() + " dB");
            SetText(lblCCE2AfGain3, afGain[4].gainMon.ToString() + " dB");

            SetText(lblRC4AfGain1, afGain[5].gainTx.ToString() + " dB");
            SetText(lblRC4AfGain2, afGain[5].gainRx.ToString() + " dB");
            SetText(lblRC4AfGain3, afGain[5].gainMon.ToString() + " dB");

            SetText(lblRepeatAfGain, afGain[6].gainTx.ToString() + " dB");

            for (int i = 0; i < 7; i++)
                muruNowData[muID].afGain[i] = afGain[i].Clone();

            SetColor(btAfGainInquiry, colorSelect);

            //AddStatus(Common.clsNMS.stationList[muID - 1] + "의 MU에서 AF Gain값을 보고하였습니다.");
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region A/S용 설정 관련(일산선)
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void ucGainSet_ILSAN_gainSetControlClick(byte btCode, byte index, byte value)
        {
            byte[] tmpBuffer = null;
            string tmpStr = "";

            switch (btCode)
            {
                case 0:     //cbSetupILSANMU_SelectedIndexChanged
                    if (ucGainSet_ILSAN.cbSetupILSANMU.SelectedIndex == 0) return;

                    if (ucGainSet_ILSAN.cbSetupILSANMU.SelectedIndex > 2)
                    {
                        SetVisible(gbMuAFGain, false);
                        SetVisible(gbILSAN_MuAFGain, true);

                        ucGainSet_ILSAN.AfGainReport(0, (byte)(ucGainSet_ILSAN.cbSetupILSANMU.SelectedIndex), muruNowData[ucGainSet_ILSAN.cbSetupILSANMU.SelectedIndex].muData_ILSAN.muGain);
                    }
                    else
                    {
                        SetVisible(gbMuAFGain, true);
                        SetVisible(gbILSAN_MuAFGain, false);
                    }

                    cbSetupMU.Text = ucGainSet_ILSAN.cbSetupILSANMU.Text;
                    break;

                case 1:     //btECHGainSet_Click
                    tmpBuffer = nmsSendDataMake.EditData_ILSAN_CTL_ECHGainValueSet((byte)ucGainSet_ILSAN.afGainSelectedIndex, 0, (byte)index, value);
                    JksSockMain.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);
                    JksSockStby.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);

                    tmpStr = Common.clsNMS.stationList[ucGainSet_ILSAN.afGainSelectedIndex - 1] + "의 MU";

                    switch (index)
                    {
                        case 0: tmpStr += " : E-CH RX AF"; break;
                        case 1: tmpStr += " : E-CH RX SQ"; break;
                        case 2: tmpStr += " : E-CH TX AF"; break;
                        case 3: tmpStr += " : E-CH TX RF"; break;
                    }

                    tmpStr += "의 Gain 값을 (" + value.ToString();

                    AddStatus(tmpStr + " ) 설정하였습니다.");
                    break;

                case 2:     //btCCHGainSet_Click
                    tmpBuffer = nmsSendDataMake.EditData_ILSAN_CTL_CCHGainValueSet((byte)ucGainSet_ILSAN.afGainSelectedIndex, 0, (byte)index, value);
                    JksSockMain.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);
                    JksSockStby.ClientSendData(tmpBuffer, (byte)tmpBuffer.Length);

                    tmpStr = Common.clsNMS.stationList[ucGainSet_ILSAN.afGainSelectedIndex - 1] + "의 MU";

                    switch (index)
                    {
                        case 0: tmpStr += " : C-CH RX AF"; break;
                        case 1: tmpStr += " : C-CH RX SQ"; break;
                        case 2: tmpStr += " : C-CH TX AF"; break;
                        case 3: tmpStr += " : C-CH TX RF"; break;
                    }

                    tmpStr += "의 Gain 값을 (" + value.ToString();

                    AddStatus(tmpStr + " ) 설정하였습니다.");
                    break;
            }
        }        
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region 경보음 재생 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        public void BellSoundPlay(string soundFileName)
        {
            try
            {
                //재생을 시작한다.
                System.Media.SoundPlayer sndPlay = new System.Media.SoundPlayer(soundFileName);
                sndPlay.Play();
            }
            catch { }
        }

        private void tmrBell_Tick(object sender, EventArgs e)
        {
            tmrBell.Stop();

            if (Common.clsNMS.flagSoundAction)
            {
                for (int i = 0; i < Common.clsNMS.stationList.Count; i++)
                {
                    if ((Common.clsNMS.flagMuError[i]) || (Common.clsNMS.flagMuError[i]))
                    {
                        BellSoundPlay(Common.clsNMS.bellFile);
                        tmrBell.Start();
                        return;
                    }

                    for (int j = 0; j < 5; j++)
                    {
                        for (int k = 0; k < 5; k++)
                        {
                            if ((Common.clsNMS.flagRuBdaError[i, j, k]) || (Common.clsNMS.flagRuFmError[i, j, k]))
                            {
                                BellSoundPlay(Common.clsNMS.bellFile);
                                tmrBell.Start();
                                return;
                            }
                        }
                    }
                }
            }

            tmrBell.Start();
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion
        
        #region NMS Server 또는 NMS 주장치와의 통신 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #region NMS Server Main
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void JksSockMain_connectEvent(Socket sock, int myID)
        {
            JksSockServer_connectEvent(0);
        }

        private void JksSockMain_dataSendReceive(int ID, bool flagSend)
        {
            JksSockServer_dataSendReceive(0, flagSend);
        }

        private void JksSockMain_dataSendReceiveDataView(int ID, bool flagSend, string strData)
        {
            JksSockServer_dataSendReceiveDataView(0, flagSend, strData);
        }

        private void JksSockMain_receivedEvent(int myID, byte[] buffer, int lng)
        {
            JksSockServer_receivedEvent(0, buffer, lng);
        }

        private void JksSockMain_closeEvent(int ID)
        {
            JksSockServer_closeEvent(0);
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region NMS Server Stby
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void JksSockStby_connectEvent(Socket sock, int myID)
        {
            JksSockServer_connectEvent(1);
        }

        private void JksSockStby_dataSendReceive(int ID, bool flagSend)
        {
            JksSockServer_dataSendReceive(1, flagSend);
        }

        private void JksSockStby_dataSendReceiveDataView(int ID, bool flagSend, string strData)
        {
            JksSockServer_dataSendReceiveDataView(1, flagSend, strData);
        }

        private void JksSockStby_receivedEvent(int myID, byte[] buffer, int lng)
        {
            JksSockServer_receivedEvent(1, buffer, lng);
        }

        private void JksSockStby_closeEvent(int ID)
        {
            JksSockServer_closeEvent(1);
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region NMS Server 통신 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void JksSockServer_connectEvent(int mainStbyID)
        {
            switch(Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "수인선":
                    AddStatus("NMS 주장치에 접속하였습니다.");
                    break;

                case "과천선":
                    if (mainStbyID == 0) AddStatus("NMS Server Main에 접속하였습니다.");
                    else AddStatus("NMS Server Stby에 접속하였습니다.");
                    break;
            }

            SetColor(lblConnectNMS, Color.Lime);
            SetColor(lblConnectServer[mainStbyID], Color.Lime);
        }

        private void JksSockServer_dataSendReceive(int mainStbyID, bool flagSend)
        {
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "과천선":
                    if (flagSend)
                    {
                        Common.clsCommon.flagTx[mainStbyID] = !Common.clsCommon.flagTx[mainStbyID];

                        if (Common.clsCommon.flagTx[mainStbyID]) SetColor(lblTxServer[mainStbyID], Color.Lime);
                        else SetColor(lblTxServer[mainStbyID], Color.Silver);
                    }
                    else
                    {
                        Common.clsCommon.flagRx[mainStbyID] = !Common.clsCommon.flagRx[mainStbyID];

                        if (Common.clsCommon.flagRx[mainStbyID]) SetColor(lblRxServer[mainStbyID], Color.Lime);
                        else SetColor(lblRxServer[mainStbyID], Color.Silver);
                    }
                    break;

                case "분당선":
                case "경의일산선":
                case "수인선":
                    if (flagSend)
                    {
                        Common.clsCommon.flagTx[mainStbyID] = !Common.clsCommon.flagTx[mainStbyID];

                        if (Common.clsCommon.flagTx[mainStbyID]) SetColor(lblTxNMS, Color.Lime);
                        else SetColor(lblTxNMS, Color.Silver);
                    }
                    else
                    {
                        Common.clsCommon.flagRx[mainStbyID] = !Common.clsCommon.flagRx[mainStbyID];

                        if (Common.clsCommon.flagRx[mainStbyID]) SetColor(lblRxNMS, Color.Lime);
                        else SetColor(lblRxNMS, Color.Silver);
                    }
                    break;
            }
        }

        private void JksSockServer_dataSendReceiveDataView(int mainStbyID, bool flagSend, string strData)
        {
            DataView(mainStbyID, flagSend, strData);
        }

        private void JksSockServer_receivedEvent(int mainStbyID, byte[] buffer, int lng)
        {
            int i = 0, j = 0;

            cntServerPolling[mainStbyID] = 0;

            if (!flagServerPolling[mainStbyID])
            {   //NMS Server Stby과의 통신복구
                flagServerPolling[mainStbyID] = true;
                SetColor(btConnectServer[mainStbyID], Color.YellowGreen);

                switch (Common.clsNMS.nmsGUIUser)
                {
                    case "분당선":
                    case "경의일산선":
                    case "수인선":
                        AddStatus("NMS 주장치 통신이상 복구");
                        break;

                    case "과천선":
                        if (mainStbyID == 0) AddStatus("NMS Server Main 통신이상 복구");
                        else AddStatus("NMS Server Stby 통신이상 복구");
                        break;
                }
            }

            #region 수신된 Data 기본처리
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            int chkSum = 0;
            byte bL = 0x00, bH = 0x00;

            //CRC 검사
            //chkSum = Common.clsCommon.MakeCRC(buffer, lng - 2);
            //bH = (byte)((chkSum & 0xFF00) >> 8);
            //bL = (byte)(chkSum & 0x00FF);

            //CheckSum 검사
            chkSum = Common.clsCommon.MakeCheckSum(buffer, lng - 2);
            bH = Common.clsCommon.HexaConvert(chkSum >> 4);
            bL = Common.clsCommon.HexaConvert(chkSum & 0x0F);

            if ((bH != buffer[lng - 2]) || (bL != buffer[lng - 1]))
            {
                //AddStatus("CRC 오류!!");
                return;
            }
            //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            #endregion
            
            j = 2;

            byte tmpMUId = buffer[j++];     //MU ID
            byte tmpRUId = buffer[j++];     //RU ID
            byte tmpCMD = buffer[j++];      //CMD
            byte tmpLng = buffer[j++];      //Data 길이
            
            //MU ID
            tmpMUId = (byte)(tmpMUId - 0x40);
            tmpRUId = (byte)(tmpRUId - 0x30);

            byte kind = 99;
            
            switch (tmpCMD)
            {
                case 0xF0:
                    #region 주/예비 절체 명령
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    byte tmpServerSt = 0;

                    /*
                    switch (mainStbyID)
                    {
                        case 0: //NMS Server Main에서 수신
                            break;

                        case 1: //NMS Server Stby에서 수신
                            break;
                    }
                    */

                    switch (buffer[6])
                    {
                        case 0x50:  //NMS Server Main에서 수신
                            switch (buffer[7])
                            {
                                case 0x50:  //NMS Sever Main이 주로 사용
                                    tmpServerSt = 1;
                                    break;

                                case 0x51:  //NMS Sever Main이 예비로 사용
                                    tmpServerSt = 2;
                                    break;
                            }
                            break;

                        case 0x51:  //NMS Server Stby에서 수신
                            switch (buffer[7])
                            {
                                case 0x50:  //NMS Sever Stby가 주로 사용
                                    tmpServerSt = 2;
                                    break;

                                case 0x51:  //NMS Sever Stby가 예비로 사용
                                    tmpServerSt = 1;
                                    break;
                            }
                            break;
                    }

                    if (serverSt != tmpServerSt)
                    {
                        serverSt = tmpServerSt;

                        switch (serverSt)
                        {
                            case 1:
                                SetColor(btServerMain, Color.YellowGreen);
                                SetColor(btServerStby, colorBase);
                                break;

                            case 2:
                                SetColor(btServerMain, colorBase);
                                SetColor(btServerStby, Color.YellowGreen);
                                break;
                        }
                    }
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    #endregion
                    break;

                case Common.clsNMSSendDataMake.CMD_TW_REPORT:   //V : 열차무선 감시 Data 보고
                    #region 열차무선 감시 Data 처리
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    bool flagILSAN_Process = false;
                    switch (Common.clsNMS.nmsGUIUser)
                    {
                        case "경의일산선":
                            if (tmpMUId > 2) flagILSAN_Process = true;
                            break;
                    }

                    if (flagILSAN_Process)
                    {
                        #region 일산선 MU Data 처리
                        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                        Common.ILSAN_MUData tmpMuData = new Common.ILSAN_MUData();

                        j += 3;

                        //동작상태 및 고장상태
                        for (i = 0; i < 8; i++)
                        {
                            //0x43 0x31 => C 1 => 0xC1 => 1100 0001
                            byte tmpValue1 = (byte)(Common.clsCommon.AHD(buffer[j++]) << 4);    //0x43 0x31 => 0xC1 
                            byte tmpValue2 = Common.clsCommon.AHD(buffer[j++]);
                            byte[] tmpBitInfoByte = Common.clsCommon.BitInfoToByte((byte)(tmpValue1 + tmpValue2)); //0xC1 => 1100 0001

                            for (int k = 0; k < 8; k++)
                                tmpMuData.muSt[i, k] = tmpBitInfoByte[k];
                        }

                        //Gain 값
                        for (i = 0; i < 8; i++)
                        {
                            //0x33 0x31 = > 0x31 => 49
                            byte tmpValue1 = (byte)(Common.clsCommon.AHD(buffer[j++]) << 4);
                            byte tmpValue2 = Common.clsCommon.AHD(buffer[j++]);
                            tmpMuData.muGain[i] = (byte)(tmpValue1 + tmpValue2);
                        }

                        j += 8;

                        tmpMuData.rfValue = Common.clsCommon.AHD(buffer[j++]) * 100 + Common.clsCommon.AHD(buffer[j++]) * 10 + Common.clsCommon.AHD(buffer[j++]);
                        tmpMuData.cchRssi = Common.clsCommon.AHD(buffer[j++]) * 100 + Common.clsCommon.AHD(buffer[j++]) * 10 + Common.clsCommon.AHD(buffer[j++]);
                        tmpMuData.echRssi = Common.clsCommon.AHD(buffer[j++]) * 100 + Common.clsCommon.AHD(buffer[j++]) * 10 + Common.clsCommon.AHD(buffer[j++]);

                        ILSAN_MuStatusReport(0, tmpMUId, tmpMuData);
                        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                        #endregion
                    }
                    else
                    {                
                        #region MU Data 처리
                        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                        if (tmpRUId == 0)
                        {   //MU Data
                            kind = buffer[j++]; //MU인지 RuA인지 구분..

                            if (kind == 0x4D)
                            {   //M : MU
                                Common.MUData tmpMuData = new Common.MUData();

                                tmpMuData.otherSt1 = Common.clsCommon.BitInfoToByte(buffer[j++]);    //기타상태1
                                tmpMuData.otherSt2 = Common.clsCommon.BitInfoToByte(buffer[j++]);    //기타상태2

                                tmpMuData.dcValue = buffer[j++];    //DC전원값

                                tmpMuData.rfValue[0] = buffer[j++];         //주 RF 출력값
                                tmpMuData.rxRssiValue[0] = buffer[j++];     //주 RX RSSI값

                                tmpMuData.rfValue[1] = buffer[j++];         //예비 RF 출력값
                                tmpMuData.rxRssiValue[1] = buffer[j++];     //예비 RX RSSI값

                                tmpMuData.rxRssiValue[2] = buffer[j++];     //감청용 RX RSSI값

                                tmpMuData.rfValueInquiry[0] = Common.clsNMS.muRfValueCalculation(buffer[j++]);  //주 RF 출력조회값
                                tmpMuData.rfValueInquiry[1] = Common.clsNMS.muRfValueCalculation(buffer[j++]);  //예비 RF 출력조회값

                                tmpMuData.chInfo[0] = buffer[j++];  //주 채널정보
                                tmpMuData.chInfo[1] = buffer[j++];  //예비 채널정보
                                tmpMuData.chInfo[2] = buffer[j++];  //감청 채널정보

                                tmpMuData.optMonSt = Common.clsCommon.BitInfoToByte(buffer[j++]);    //통신 감시 상태

                                tmpMuData.chReturn = buffer[j++];   //채널 복귀 상태

                                tmpMuData.pllLockErrorSt = Common.clsCommon.BitInfoToByte(buffer[j++]);  //PLL Lock 장애 상태

                                j++;    //Testmode

                                tmpMuData.revelValue = buffer[j++]; //level 측정값

                                tmpMuData.pttSt = Common.clsCommon.BitInfoToByte(buffer[j++]);    //PTT 상태

                                tmpMuData.lifSt = Common.clsCommon.BitInfoToByte(buffer[j++]);    //LIF 상태

                                tmpMuData.powerSt = Common.clsCommon.BitInfoToByte(buffer[j++]);        //전원 상태

                                switch (Common.clsNMS.nmsGUIUser)
                                {
                                    case "분당선":
                                        if (Common.clsNMS.presentMUID > 5)
                                        {   //기존 분당선에 적용하면 안됨.
                                            tmpMuData.mainVer = buffer[j++];    //역용 주 버젼정보
                                            tmpMuData.stbyVer = buffer[j++];    //역용 예비 버젼정보
                                            tmpMuData.acuVer = buffer[j++];     //ACU 버젼정보
                                            tmpMuData.wnSt = Common.clsCommon.BitInfoToByte(buffer[j++]);   //무전기별 상태(Wide/Narrow)
                                            tmpMuData.monVer = buffer[j++];     //감청용 버젼정보
                                            tmpMuData.optDcValue = buffer[j++]; //DC 전원값

                                            DC_RangeOver(0, tmpMUId, tmpRUId, tmpMuData.optDcValue);    //DC 전압 임계치 비고
                                        }
                                        break;

                                    case "수인선":
                                    case "과천선":
                                        tmpMuData.mainVer = buffer[j++];    //역용 주 버젼정보
                                        tmpMuData.stbyVer = buffer[j++];    //역용 예비 버젼정보
                                        tmpMuData.acuVer = buffer[j++];     //ACU 버젼정보
                                        tmpMuData.wnSt = Common.clsCommon.BitInfoToByte(buffer[j++]);   //무전기별 상태(Wide/Narrow)
                                        tmpMuData.monVer = buffer[j++];     //감청용 버젼정보
                                        tmpMuData.optDcValue = buffer[j++]; //DC 전원값

                                        DC_RangeOver(0, tmpMUId, tmpRUId, tmpMuData.optDcValue);    //DC 전압 임계치 비고
                                        break;
                                }

                                RF_RangeOver(tmpMUId, tmpMuData);   //송신출력 임계치 비교

                                MuStatusReport(0, tmpMUId, tmpMuData);
                            }
                            else if (kind == 0x41)
                            {   //A : RU A형
                                Common.RUDetailData tmpRuAData = new Common.RUDetailData();

                                tmpRuAData.otherSt1 = Common.clsCommon.BitInfoToByte(buffer[j++]);   //기타상태1
                                tmpRuAData.otherSt2 = Common.clsCommon.BitInfoToByte(buffer[j++]);   //기타상태2

                                tmpRuAData.dcValue = buffer[j++];    //DC전원값

                                tmpRuAData.rfValue = buffer[j++]; //주 RF 출력값
                                tmpRuAData.rfValueInquiry = Common.clsNMS.ruRfValueCalculation(buffer[j++]); //주 RF 출력조회값

                                tmpRuAData.rxRssiValue = buffer[j++];    //예비 RX RSSI 감도
                                tmpRuAData.repeatPtt = buffer[j++];     //REPEAT PTT

                                tmpRuAData.optOtherSt = Common.clsCommon.BitInfoToByte(buffer[j++]);   //OPT 기타상태
                                tmpRuAData.optDcValue = buffer[j++];   //OPT DC값
                                tmpRuAData.optAlarmSt = Common.clsCommon.BitInfoToByte(buffer[j++]);   //OPT 알람상태

                                DC_RangeOver(1, tmpMUId, tmpRUId, tmpRuAData.dcValue);

                                RF_RangeOver(tmpMUId, tmpRUId, tmpRuAData);   //송신출력 임계치 비교

                                RuStatusReport(0, tmpMUId, tmpRUId, tmpRuAData);
                            }
                        }
                        else
                        {   //RU Data
                            Common.RUDetailData tmpRuBData = new Common.RUDetailData();

                            tmpRuBData.otherSt1 = Common.clsCommon.BitInfoToByte(buffer[j++]);   //기타상태1
                            tmpRuBData.otherSt2 = Common.clsCommon.BitInfoToByte(buffer[j++]);   //기타상태2

                            tmpRuBData.dcValue = buffer[j++];    //DC전원값

                            tmpRuBData.rfValue = buffer[j++]; //주 RF 출력값
                            tmpRuBData.rfValueInquiry = Common.clsNMS.ruRfValueCalculation(buffer[j++]); //주 RF 출력조회값

                            tmpRuBData.rxRssiValue = buffer[j++];    //예비 RX RSSI 감도

                            DC_RangeOver(2, tmpMUId, tmpRUId, tmpRuBData.dcValue);

                            RF_RangeOver(tmpMUId, tmpRUId, tmpRuBData);   //송신출력 임계치 비교

                            RuStatusReport(0, tmpMUId, tmpRUId, tmpRuBData);
                        }
                        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                        #endregion
                    }
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    #endregion
                    break;

                case Common.clsNMSSendDataMake.CMD_BaseIF_REPORT:   //I : Base I/F 감시 Data 보고
                    j += 13;
                    
                    muruNowData[tmpMUId].baseIFData_ILSAN.dcValue = buffer[j++];
                    j++;
                    muruNowData[tmpMUId].baseIFData_ILSAN.fmSt = buffer[j++];

                    DC_RangeOver(0, tmpMUId, tmpRUId, muruNowData[tmpMUId].baseIFData_ILSAN.dcValue);    //DC 전압 임계치 비고
                    break;

                case Common.clsNMSSendDataMake.CMD_FM_REPORT:   //F :FM 감시 Data 보고
                    #region FM 감시 Data 처리
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    switch (Common.clsNMS.nmsGUIUser)
                    {
                        case "경의일산선":
                            if (tmpMUId <= 2)
                                if (tmpRUId == 0) kind = buffer[j++];   //MU인지 RuA인지 구분..
                            break;

                        default:
                            if (tmpRUId == 0) kind = buffer[j++];   //MU인지 RuA인지 구분..
                            break;
                    }

                    Common.FM_Info tmpFmInfo = new Common.FM_Info();

                    //소스텔의 FM 단독형 장치
                    tmpFmInfo.acVoltage = buffer[j++];
                    tmpFmInfo.acCurrent = buffer[j++];
                    j += 6;
                    tmpFmInfo.betteryVoltage = buffer[j++];
                    tmpFmInfo.betteryChangeCurrent = buffer[j++];
                    tmpFmInfo.acInputFail = buffer[j++];
                    tmpFmInfo.dcOutputFail = buffer[j++];
                    j++;
                    tmpFmInfo.ldAlarm = buffer[j++];
                    tmpFmInfo.ldPower = (sbyte)buffer[j++];
                    tmpFmInfo.pdAlarm = buffer[j++];
                    tmpFmInfo.pdPower = (sbyte)buffer[j++];
                    tmpFmInfo.rssi = (sbyte)buffer[j++];
                    tmpFmInfo.temperature = (sbyte)buffer[j++];
                    tmpFmInfo.tssiFwd = (sbyte)buffer[j++];
                    tmpFmInfo.tssiRev = (sbyte)buffer[j++];

                    if(kind == 99) MuFmStReport(0, tmpMUId, tmpRUId, tmpFmInfo);
                    else if (kind == 0x4D) MuFmStReport(0, tmpMUId, tmpRUId, tmpFmInfo);
                    else RuFmStReport(0, tmpMUId, tmpRUId, tmpFmInfo);
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    #endregion
                    break;

                case Common.clsNMSSendDataMake.CMD_AFGain:    //Q : CCE, RC1, RC2, RC3, CCE2, RC4, Repeat의 TX/RX/MON AF Gain값 요청에 대한 응답
                    #region CCE, RC1, RC2, RC3, CCE2, RC4, Repeat의 TX/RX/MON AF Gain값 요청에 대한 응답 처리
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    j++;    //예비

                    Common.AFGain[] tmpAfGain = new Common.AFGain[7];

                    for (i = 0; i < 6; i++)
                    {
                        tmpAfGain[i] = new Common.AFGain();

                        tmpAfGain[i].gainTx = buffer[j++];
                        tmpAfGain[i].gainRx = buffer[j++];
                        tmpAfGain[i].gainMon = buffer[j++];
                    }

                    //Repeat 값
                    tmpAfGain[6] = new Common.AFGain();
                    tmpAfGain[6].gainTx = buffer[j++];

                    AfGainReport(0, tmpMUId, tmpAfGain);
                    //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                    #endregion
                    break;
            }
        }

        private void JksSockServer_closeEvent(int mainStbyID)
        {
            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    AddStatus("NMS 주장치와의 접속이 끊겼습니다.");
                    break;

                case "과천선":
                    if (mainStbyID == 0) AddStatus("NMS Server Main과의 접속이 끊겼습니다.");
                    else AddStatus("NMS Server Stby와의 접속이 끊겼습니다.");
                    break;
            }

            SetColor(lblConnectNMS, Color.Silver);
            SetColor(lblConnectServer[mainStbyID], Color.Silver);
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        //송수신 Data 보기
        private void DataView(int mainStbyID, bool flagSend, byte[] buffer, int lng)
        {
            if (frmDV != null)
            {
                string strData = null;

                for (int i = 0; i < lng; i++)
                    strData += string.Format("{0:X2} ", buffer[i]);

                if (flagSend) frmDV.SetTxData(mainStbyID, strData);
                else frmDV.SetRxData(mainStbyID, strData);
            }
        }

        private void DataView(int mainStbyID, bool flagSend, string strData)
        {
            if (frmDV != null)
            {
                if (flagSend) frmDV.SetTxData(mainStbyID, strData);
                else frmDV.SetRxData(mainStbyID, strData);
            }
        }

        private byte DC_RangeOver(int mode, int muID, int ruID, byte dcValue)
        {
            double tmpValue = dcValue / 10.0;
            if ((tmpValue < Common.clsNMS.rangeValue[2]) || (tmpValue > Common.clsNMS.rangeValue[3]))
            {
                //수정일 : 2016-02-29
                //검수용
                //SetVisible(frmErrMsg, true);

                switch (mode)
                {
                    case 0: //MU
                        if (!Common.clsNMS.flagMuError[muID - 1])
                        {   //정상에서 Error가 발생한 상태이므로 메세지창을 띄운다.


                            //수정일 : 2016-02-29
                            //일단 검수를 위해 안보이게..
                            //FIXME 검수 후 풀어야함
                            //frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)의 DC 전압(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                            //AddStatus(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)의 DC 전압(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                            //Common.clsNMS.flagMuError[muID - 1] = true;
                        }
                        return 1;

                    case 1: //RU_A
                        SetVisible(frmErrMsg, true);

                        if (!Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0])
                        {
                            frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 광중계장치(RU_A)의 DC 전압(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                            AddStatus(Common.clsNMS.stationList[muID - 1] + " 광중계장치(RU_A)의 DC 전압(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");

                            Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0] = true;
                        }
                        return 1;

                    case 2: //RU_B

                        SetVisible(frmErrMsg, true);

                        if (!Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0])
                        {
                            frmErrMsg.SetText(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(RU_B)의 DC 전압(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                            AddStatus(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(RU_B)의 DC 전압(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");

                            Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0] = true;
                        }
                        return 1;
                }                
            }

            return 0;
        }

        private void RF_RangeOver(int muID, Common.MUData tmpMuData)
        {
            double tmpValue = 0.0;

            //PTT가 OFF될때에 임계치를 비교한다.
            if (tmpMuData.otherSt2[2] == 0)
            {   //PTT OFF
                if (muruNowData[muID].muData.otherSt2[2] != 0)
                {   //PTT ON. 즉, 이전까지 PTT ON이었다가 OFF가 되는 시점에서 임계치를 비교한다.
                    //tmpValue = tmpMuData.rfValue[0] / 10.0;
                    tmpValue = muruNowData[muID].muData.rfValue[0] / 10.0;  //이전의 RF 출력값을 비교한다.
                    
                    if ((tmpValue < Common.clsNMS.rangeValue[0]) || (tmpValue > Common.clsNMS.rangeValue[1]))
                    {
                        SetVisible(frmErrMsg, true);
                        frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)의 Main RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                        AddStatus(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)의 Main RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                    }
                }
            }

            //PTT가 OFF될때에 임계치를 비교한다.
            if (tmpMuData.otherSt2[4] == 0)
            {   //PTT OFF
                if (muruNowData[muID].muData.otherSt2[4] != 0)
                {   //PTT ON. 즉, 이전까지 PTT ON이었다가 OFF가 되는 시점에서 임계치를 비교한다.
                    //tmpValue = tmpMuData.rfValue[1] / 10.0;
                    tmpValue = muruNowData[muID].muData.rfValue[1] / 10.0;  //이전의 RF 출력값을 비교한다.

                    if ((tmpValue < Common.clsNMS.rangeValue[0]) || (tmpValue > Common.clsNMS.rangeValue[1]))
                    {
                        SetVisible(frmErrMsg, true);
                        frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)의 Stby RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                        AddStatus(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)의 Stby RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                    }
                }
            }
        }

        private void RF_RangeOver(int muID,int ruID, Common.RUDetailData tmpRuData)
        {
            //PTT가 OFF될때에 임계치를 비교한다.
            if (tmpRuData.otherSt1[6] == 0)
            {   //PTT OFF
                if (muruNowData[muID].ruData[ruID].ruData.otherSt1[6] != 0)
                {   //PTT ON. 즉, 이전까지 PTT ON이었다가 OFF가 되는 시점에서 임계치를 비교한다.                    
                    double tmpValue = tmpRuData.rfValue / 10.0;

                    if ((tmpValue < Common.clsNMS.rangeValue[0]) || (tmpValue > Common.clsNMS.rangeValue[1]))
                    {
                        SetVisible(frmErrMsg, true);

                        if (ruID == 0)
                        {   //RU A형
                            frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 광중계장치(RU_A)의 RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                            AddStatus(Common.clsNMS.stationList[muID - 1] + " 광중계장치(RU_A)의 RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                        }
                        else
                        {   //RU B형
                            frmErrMsg.SetText(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(RU_B)의 RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                            AddStatus(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(RU_B)의 RF 출력값(" + tmpValue.ToString() + ")이 범위를 벗어났습니다.");
                        }
                    }
                }
            }
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        #region DataBase 저장 관련
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        private void nmsMUSt_DBSave(byte muID)
        {
            Common.MuInfo tmpMuInfo = new Common.MuInfo();

            tmpMuInfo.mu_id = muID;

            tmpMuInfo.autoManual = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[0]);
            tmpMuInfo.mainStby = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[1]);
            tmpMuInfo.wideNarrow = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[2]);
            //tmpMuInfo.ac = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[3]);
            //tmpMuInfo.dc = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[4]);
            //tmpMuInfo.trxUnit = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[5]);

            //CCE PTT
            tmpMuInfo.cce1Ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[6]);

            tmpMuInfo.rc1Ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt1[7]);

            tmpMuInfo.rc2Ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[0]);
            tmpMuInfo.rc3Ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[1]);
            tmpMuInfo.mainPtt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[2]);
            tmpMuInfo.mainBusy = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[3]);
            tmpMuInfo.stbyPtt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[4]);
            tmpMuInfo.stbyBusy = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[5]);
            tmpMuInfo.monBusy = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[6]);
            tmpMuInfo.acuCard = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.otherSt2[7]);

            //tmpMuInfo.dcValue = (float)(muruNowData[muID].muData.dcValue / 10.0);

            tmpMuInfo.mainRf = (float)(muruNowData[muID].muData.rfValue[0] / 10.0);
            tmpMuInfo.mainRssi = (short)(muruNowData[muID].muData.rxRssiValue[0] * -1);
            tmpMuInfo.stbyRf = (float)(muruNowData[muID].muData.rfValue[1] / 10.0);
            tmpMuInfo.stbyRssi = (short)(muruNowData[muID].muData.rxRssiValue[1] * -1);
            tmpMuInfo.monRssi = (short)(muruNowData[muID].muData.rxRssiValue[2] * -1);
            tmpMuInfo.mainRfSet = muruNowData[muID].muData.rfValueInquiry[0];
            tmpMuInfo.stbyRfSet = muruNowData[muID].muData.rfValueInquiry[1];

            tmpMuInfo.mainCh = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.chInfo[0]);
            tmpMuInfo.stbyCh = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.chInfo[1]);
            tmpMuInfo.monCh = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.chInfo[2]);

            tmpMuInfo.mainComm = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.optMonSt[0]);
            tmpMuInfo.stbyComm = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.optMonSt[1]);
            tmpMuInfo.monComm = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.optMonSt[2]);
            //tmpMuInfo.muTrxCpu = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.optMonSt[3]);
            //tmpMuInfo.muOptCpu = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.optMonSt[4]);
            //tmpMuInfo.MuUps = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.optMonSt[5]);

            tmpMuInfo.chReturn = muruNowData[muID].muData.chReturn;

            tmpMuInfo.mainPllLock = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pllLockErrorSt[0]);
            tmpMuInfo.stbyPllLock = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pllLockErrorSt[1]);
            tmpMuInfo.monPllLock = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pllLockErrorSt[2]);

            tmpMuInfo.ruASt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pllLockErrorSt[3]);

            tmpMuInfo.cce2Ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pttSt[0]);
            tmpMuInfo.rc4Ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pttSt[1]);
            tmpMuInfo.repeatPtt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.pttSt[2]);

            tmpMuInfo.lif1 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.lifSt[0]);
            tmpMuInfo.lif2 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.lifSt[1]);
            tmpMuInfo.lif3 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.lifSt[2]);
            tmpMuInfo.lif4 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.lifSt[3]);
            tmpMuInfo.lif5 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.lifSt[4]);
            tmpMuInfo.lif6 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.lifSt[5]);

            tmpMuInfo.powerMain = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.powerSt[0]);
            tmpMuInfo.powerStby = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.powerSt[1]);

            tmpMuInfo.dc = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData.wnSt[3]);

            dbMuQueue.Enqueue(tmpMuInfo);
            //fbDBMS_NMS.MuInsert(tmpMuInfo); //DB에 저장
        }
        
        private void nmsMUSt_ILSAN_DBSave(byte muID)
        {
            //muruNowData[muID].muData_ILSAN
            
            Common.ILSAN_MUInfo tmpMuInfo = new Common.ILSAN_MUInfo();

            tmpMuInfo.mu_id = muID;

            tmpMuInfo.mainCchTxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 0]);
            tmpMuInfo.mainCchRxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 1]);
            tmpMuInfo.mainEchTxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 2]);
            tmpMuInfo.mainEchRxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 3]);
            tmpMuInfo.mainCchTx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 4]);
            tmpMuInfo.mainCchRx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 5]);
            tmpMuInfo.mainEchTx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 6]);
            tmpMuInfo.mainEchRx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[0, 7]);

            tmpMuInfo.stbyCchTxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 0]);
            tmpMuInfo.stbyCchRxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 1]);
            tmpMuInfo.stbyEchTxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 2]);
            tmpMuInfo.stbyEchRxFault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 3]);
            tmpMuInfo.stbyCchTx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 4]);
            tmpMuInfo.stbyCchRx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 5]);
            tmpMuInfo.stbyEchTx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 6]);
            tmpMuInfo.stbyEchRx = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[1, 7]);

            tmpMuInfo.cpu1 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[3, 5]);
            tmpMuInfo.cpu2 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[3, 6]);
            tmpMuInfo.cpu1Fault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[4, 0]);
            tmpMuInfo.cpu2Fault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[4, 1]);

            tmpMuInfo.power1 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[4, 2]);
            tmpMuInfo.power2 = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[4, 3]);
            tmpMuInfo.power1Fault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[4, 4]);
            tmpMuInfo.power2Fault = (char)Common.clsCommon.HexaConvert(muruNowData[muID].muData_ILSAN.muSt[4, 5]);

            tmpMuInfo.rfValue = (float)(muruNowData[muID].muData_ILSAN.rfValue / 10.0);
            tmpMuInfo.cchRssi = (short)(muruNowData[muID].muData_ILSAN.cchRssi * -1);
            tmpMuInfo.echRssi = (short)(muruNowData[muID].muData_ILSAN.echRssi * -1);
            tmpMuInfo.dcValue = (float)(muruNowData[muID].baseIFData_ILSAN.dcValue / 10.0);

            dbMuQueue_ILSAN.Enqueue(tmpMuInfo);
        }

        private void nmsMuFmSt_DBSave(byte muID)
        {
            muruNowData[muID].fmInfo.mu_id = muID;

            dbMuFmQueue.Enqueue(muruNowData[muID].fmInfo);
        }

        private void nmsRUSt_DBSave(byte muID, byte ruID)
        {
            Common.RuInfo tmpRuInfo = new Common.RuInfo();

            tmpRuInfo.mu_id = muID;
            tmpRuInfo.ru_id = ruID;

            tmpRuInfo.autoManual = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[0]);
            tmpRuInfo.mainStby = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[1]);
            tmpRuInfo.wideNarrow = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[2]);
            tmpRuInfo.ac = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[3]);
            tmpRuInfo.dc = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[4]);
            tmpRuInfo.modOpen = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[5]);
            tmpRuInfo.ptt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[6]);
            tmpRuInfo.localRemote = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt1[7]);

            tmpRuInfo.ruCpu = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[0]);
            tmpRuInfo.controlCpu = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[0]);
            tmpRuInfo.data = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[1]);
            tmpRuInfo.optLd = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[3]);
            tmpRuInfo.optPd = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[4]);
            tmpRuInfo.xmtrPllLock = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[5]);
            tmpRuInfo.rcvrpllLock = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[6]);
            tmpRuInfo.ac_Stby = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.otherSt2[7]);

            tmpRuInfo.dcValue = (float)(muruNowData[muID].ruData[ruID].ruData.dcValue / 10.0);

            tmpRuInfo.rf = (float)(muruNowData[muID].ruData[ruID].ruData.rfValue / 10.0);
            tmpRuInfo.rfSet = muruNowData[muID].ruData[ruID].ruData.rfValueInquiry;
            tmpRuInfo.rssi = (short)(muruNowData[muID].ruData[ruID].ruData.rxRssiValue * -1);

            if (ruID == 0)
            {
                tmpRuInfo.repeatPtt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.repeatPtt);

                tmpRuInfo.optDc = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optOtherSt[0]);
                tmpRuInfo.optModOpen = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optOtherSt[1]);
                tmpRuInfo.optPtt = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optOtherSt[2]);

                tmpRuInfo.optDcValue = (float)(muruNowData[muID].ruData[ruID].ruData.optDcValue / 10.0);

                tmpRuInfo.opt1Ld = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[0]);
                tmpRuInfo.opt2Ld = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[1]);
                tmpRuInfo.opt3Ld = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[2]);
                tmpRuInfo.opt4Ld = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[3]);
                tmpRuInfo.opt1Pd = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[4]);
                tmpRuInfo.opt2Pd = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[5]);
                tmpRuInfo.opt3Pd = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[6]);
                tmpRuInfo.opt4Pd = (char)Common.clsCommon.HexaConvert(muruNowData[muID].ruData[ruID].ruData.optAlarmSt[7]);

                dbRuAQueue.Enqueue(tmpRuInfo);
                //fbDBMS_NMS.RuAInsert(tmpRuInfo); //DB에 저장
            }
            else
                dbRuBQueue.Enqueue(tmpRuInfo);
            //fbDBMS_NMS.RuInsert(tmpRuInfo); //DB에 저장
        }

        private void nmsRuFmSt_DBSave(byte muID, byte ruID)
        {
            muruNowData[muID].ruData[ruID].fmInfo.mu_id = muID;
            muruNowData[muID].ruData[ruID].fmInfo.ru_id = ruID;

            dbRuFmQueue.Enqueue(muruNowData[muID].ruData[ruID].fmInfo);
        }

        private void tmrDBSave_Tick(object sender, EventArgs e)
        {
            tmrDBSave.Stop();

            if (gbDB.Visible)
            {
                SetText(tbDBCnt1, dbMuQueue.Count.ToString());
                SetText(tbDBCnt2, dbRuAQueue.Count.ToString());
                SetText(tbDBCnt3, dbRuBQueue.Count.ToString());
                SetText(tbDBCnt4, dbMuFmQueue.Count.ToString());
                SetText(tbDBCnt5, dbRuFmQueue.Count.ToString());
            }

            if (dbMuQueue.Count > 0) fbDBMS_NMS.MuInsert((Common.MuInfo)dbMuQueue.Dequeue());
            if (dbMuQueue_ILSAN.Count > 0) fbDBMS_NMS.MuInsert_ILSAN((Common.ILSAN_MUInfo)dbMuQueue_ILSAN.Dequeue());
            if (dbRuAQueue.Count > 0) fbDBMS_NMS.RuAInsert((Common.RuInfo)dbRuAQueue.Dequeue());
            if (dbRuBQueue.Count > 0) fbDBMS_NMS.RuInsert((Common.RuInfo)dbRuBQueue.Dequeue());
            if (dbMuFmQueue.Count > 0) fbDBMS_NMS.MuFmInsert((Common.FM_Info)dbMuFmQueue.Dequeue());
            if (dbRuFmQueue.Count > 0) fbDBMS_NMS.RuFmInsert((Common.FM_Info)dbRuFmQueue.Dequeue());

            tmrDBSave.Start();
        }

        private void panelGround3_DoubleClick(object sender, EventArgs e)
        {
            SetVisible(gbDB, !gbDB.Visible);
        }
        //'''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        #endregion

        private void nmsMUSt_Display(byte muID)
        {
            if (Common.clsNMS.muruComSt[muID - 1].flagMuFirst)
            {
                if (flagILSAN) ucMUSt_ILSAN.nmsMUSt_Display(muID, muruNowData[muID].muData_ILSAN, muruNowData[muID].baseIFData_ILSAN);
                else ucMUSt.nmsMUSt_Display(muID, muruNowData[muID].muData);
            }
            else    //Data가 한번도 수신되지 않은경우(처음 프로그램을 실행했을 경우..)
            {
                if (flagILSAN) ucMUSt_ILSAN.MuStInit(false);
                else ucMUSt.MuStInit(false);
            }
        }

        private void nmsFMSt_Display(byte muID)
        {
            if (Common.clsNMS.muruComSt[muID - 1].flagFmFirst)
            {
                if (flagILSAN) ucMUSt_ILSAN.nmsFMSt_Display(muruNowData[muID].fmInfo);
                else ucMUSt.nmsFMSt_Display(muruNowData[muID].fmInfo);
            }
            else    //Data가 한번도 수신되지 않은경우(처음 프로그램을 실행했을 경우..)
            {
                if (flagILSAN) ucMUSt_ILSAN.FmStInit(false);
                else ucMUSt.FmStInit(false);
            }
        }

        private void nmsRUSt_Display(byte muID, byte ruID)
        {
            if (Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagRuFirst)
                ucRUSt.nmsRUSt_Display(muruNowData[muID].ruData[ruID].ruData, Common.clsNMS.muruName[muID - 1].ruName);
            else
                ucRUSt.RuStInit(false);
        }

        private void nmsFMSt_Display(byte muID, byte ruID)
        {
            if (Common.clsNMS.muruComSt[muID - 1].ruBdaCommSt[ruID].flagFmFirst)
                ucRUSt.nmsFMSt_Display(muruNowData[muID].ruData[ruID].fmInfo);
            else    //Data가 한번도 수신되지 않은경우(처음 프로그램을 실행했을 경우..)
                ucRUSt.FmStInit(false);
        }

        private void nmsMUError_Display(byte muID)
        {   //전체화면에 주예비 상태 및 장애 표시
            int tmpResult = 0;
            int i = 0;

            //MU의 장애 표시
            tmpResult =
                //MU의 장애 상태 분석
                //muruNowData[muID].muData.otherSt1[3] +      //AC 전원
                //muruNowData[muID].muData.otherSt1[4] +      //DC 전원
                //muruNowData[muID].muData.otherSt1[5] +      //ModOpen(Monitor unit - TRX unit)
                muruNowData[muID].muData.otherSt2[7] +          //ACU 카드 상태

                muruNowData[muID].muData.optMonSt[0] +      //주 통신감시(Main Transceiver)
                muruNowData[muID].muData.optMonSt[1] +		//예비 통신감시(Stby Transceiver)
                muruNowData[muID].muData.optMonSt[2];       //감청통신감시(Monitor Receiver)
            //muruNowData[muID].muData.optMonSt[3] +      //MU TRX CPU
            //muruNowData[muID].muData.optMonSt[4] +      //MU OPT CPU
            //muruNowData[muID].muData.optMonSt[5] +      //MU UPS

            //muruNowData[muID].muData.optOtherSt[0] +    //OPT DC 전원
            //muruNowData[muID].muData.optOtherSt[1];     //OPT ModOpen

            /*
            for (i = 0; i < 4; i++)
            {
                if (clsCommon.muruName[muID - 1].ruName[i].ruName != "")
                {
                    tmpResult += muruNowData[muID].muData.optAlarmSt[i] +       //OPT 1 ~ 4 LD
                                 muruNowData[muID].muData.optAlarmSt[i + 4];    //OPT 1 ~ 4 PD
                }
            }
            */

            tmpResult +=
                muruNowData[muID].muData.pllLockErrorSt[0] +    //주 PLL LOCK 장애
                muruNowData[muID].muData.pllLockErrorSt[1] +    //예비 PLL LOCK 장애
                muruNowData[muID].muData.pllLockErrorSt[2] +    //감청 PLL LOCK 장애
                muruNowData[muID].muData.pllLockErrorSt[3];     //RUA 상태

            //muruNowData[muID].upsData.otherSt[4] +      //입력전원(UPS Failed)
            //muruNowData[muID].upsData.otherSt[5] +      //Bypass
            //muruNowData[muID].upsData.otherSt[6] +      //Battery Low
            //muruNowData[muID].upsData.otherSt[7];       //Utility Fail

            //LIF 장애
            for (i = 0; i < 6; i++)
                tmpResult += muruNowData[muID].muData.lifSt[i];

            //POWER 장애
            tmpResult += muruNowData[muID].muData.powerSt[0];   //Main 전원
            tmpResult += muruNowData[muID].muData.powerSt[1];   //Stby 전원

            tmpResult += muruNowData[muID].muData.wnSt[3];      //DC 전원 장애

            //Auto인데 Stby이면 장애
            if ((muruNowData[muID].muData.otherSt1[0] == 0) && (muruNowData[muID].muData.otherSt1[1] == 1))
                tmpResult++;

            //전체화면의 MU버튼에 이상여부 표시
            if (tmpResult == 0)
            {
                SetColor(btMu[muID - 1], colorSelect);
                SetForeColor(btMu[muID - 1], Color.Black);
            }
            else
            {
                SetColor(btMu[muID - 1], colorError);
                SetForeColor(btMu[muID - 1], Color.White);
            }

            //Error 상태 저장
            if (tmpResult == 0) Common.clsNMS.flagMuError[muID - 1] = false;
            else
            {   //Error 발생
                if (!Common.clsNMS.flagMuError[muID - 1])
                {   //정상에서 Error가 발생한 상태이므로 메세지창을 띄운다.
                    SetVisible(frmErrMsg, true);
                    frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                        + Common.clsNMS.stationList[muID - 1] + " 기지국 상세 화면을\r\n확인하시기 바랍니다.");

                    AddStatus(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)에 장애가 발생하였습니다.");
                }

                Common.clsNMS.flagMuError[muID - 1] = true;
            }
        }

        private void nmsMUError_ILSAN_Display(byte muID)
        {   //전체화면에 주예비 상태 및 장애 표시
            int tmpResult = 0;
            int i = 0;

            //MU의 장애 표시
            tmpResult =
                muruNowData[muID].muData_ILSAN.muSt[0, 0] +     //C-CH 송/수신 장애
                muruNowData[muID].muData_ILSAN.muSt[0, 1] +
                muruNowData[muID].muData_ILSAN.muSt[0, 2] +
                muruNowData[muID].muData_ILSAN.muSt[0, 3] +

                muruNowData[muID].muData_ILSAN.muSt[1, 0] +     //E-CH 송/수신 장애
                muruNowData[muID].muData_ILSAN.muSt[1, 1] +
                muruNowData[muID].muData_ILSAN.muSt[1, 2] +
                muruNowData[muID].muData_ILSAN.muSt[1, 3] +

                muruNowData[muID].muData_ILSAN.muSt[4, 0] +     //CPU 장애
                muruNowData[muID].muData_ILSAN.muSt[4, 1] +
                muruNowData[muID].muData_ILSAN.muSt[4, 4] +     //POWER 장애
                muruNowData[muID].muData_ILSAN.muSt[4, 5];

            tmpResult += muruNowData[muID].baseIFData_ILSAN.fmSt;

            //전체화면의 MU버튼에 이상여부 표시
            if (tmpResult == 0)
            {
                SetColor(btMu[muID - 1], colorSelect);
                SetForeColor(btMu[muID - 1], Color.Black);
            }
            else
            {
                SetColor(btMu[muID - 1], colorError);
                SetForeColor(btMu[muID - 1], Color.White);
            }

            //Error 상태 저장
            if (tmpResult == 0) Common.clsNMS.flagMuError[muID - 1] = false;
            else
            {   //Error 발생
                if(!Common.clsNMS.flagMuError[muID - 1])
                {   //정상에서 Error가 발생한 상태이므로 메세지창을 띄운다.
                    SetVisible(frmErrMsg, true);
                    frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                        + Common.clsNMS.stationList[muID - 1] + " 기지국 상세 화면을\r\n확인하시기 바랍니다.");

                    AddStatus(Common.clsNMS.stationList[muID - 1] + " 기지국(MU)에 장애가 발생하였습니다.");
                }

                Common.clsNMS.flagMuError[muID - 1] = true;     
            }
        }

        private void nmsMuFmError_Display(byte muID)
        {   //전체화면에 주예비 상태 및 장애 표시
            /*
            //FM이 없으면 정상처리한다.
            if (!Common.clsNMS.muruFmExist[muID - 1].flagMu) return 0;
            */

            //FM의 장애 상태 분석
            int tmpResult =
                    muruNowData[muID].fmInfo.acInputFail +
                    muruNowData[muID].fmInfo.dcOutputFail +
                    muruNowData[muID].fmInfo.ldAlarm +
                    muruNowData[muID].fmInfo.pdAlarm;

            //전체화면의 MU버튼에 이상여부 표시
            if (tmpResult == 4)
            {
                SetColor(btMuFm[muID - 1], colorSelect);
                SetForeColor(btMuFm[muID - 1], Color.Black);
            }
            else
            {
                SetColor(btMuFm[muID - 1], colorError);
                SetForeColor(btMuFm[muID - 1], Color.White);
            }

            //Error 상태 저장
            if (tmpResult == 4) Common.clsNMS.flagMuFmError[muID - 1] = false;
            else
            {   //Error 발생
                if (!Common.clsNMS.flagMuFmError[muID - 1])
                {   //정상에서 Error가 발생한 상태이므로 메세지창을 띄운다.
                    //SetVisible(frmErrMsg, true);
                    //frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 기지국(FM)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                    //    + Common.clsNMS.stationList[muID - 1] + " 기지국 상세 화면을\r\n확인하시기 바랍니다.");

                    //AddStatus(Common.clsNMS.stationList[muID - 1] + " 기지국(FM)에 장애가 발생하였습니다.");
                }

                Common.clsNMS.flagMuFmError[muID - 1] = true;     //Error 발생
            }
        }

        private void nmsRUError_Display(byte muID, byte ruID)
        {
            //RU의 장애 표시
            int tmpResult =
                //RU의 장애 상태 분석
                muruNowData[muID].ruData[ruID].ruData.otherSt1[3] +       //AC
                muruNowData[muID].ruData[ruID].ruData.otherSt1[4] +       //DC
                muruNowData[muID].ruData[ruID].ruData.otherSt1[5] +       //ModOpen

                muruNowData[muID].ruData[ruID].ruData.otherSt2[0] +       //RU CPU(Control CPU)
                //muruNowData[muID].ruData[ruID].ruData.otherSt2[1] +       //데이터 광접속
                //muruNowData[muID].ruData[ruID].ruData.otherSt2[3] +       //OPT LD 알람
                muruNowData[muID].ruData[ruID].ruData.otherSt2[5] +       //XMTR PLL LOCK 장애
                muruNowData[muID].ruData[ruID].ruData.otherSt2[6] +        //RCVR PLL LOCK 장애
                muruNowData[muID].ruData[ruID].ruData.otherSt2[7];        //예비전원

            if (ruID == 0)
            {	//RuA이면 장애추가
                tmpResult += muruNowData[muID].ruData[ruID].ruData.optOtherSt[0] +	//OPT DC
                             muruNowData[muID].ruData[ruID].ruData.optOtherSt[1];	//OPT MODOpen

                int i = 0;
                for (i = 0; i < 8; i++)
                    tmpResult += muruNowData[muID].ruData[ruID].ruData.optAlarmSt[i];
            }

            //Auto인데 Stby이면 장애
            if ((muruNowData[muID].ruData[ruID].ruData.otherSt1[0] == 0) && (muruNowData[muID].ruData[ruID].ruData.otherSt1[1] == 1))
                tmpResult++;

            //전체화면의 RU버튼에 이상여부 표시
            if (ruID == 0)
            {	//RuA
                try
                {
                    if (tmpResult == 0)
                    {
                        SetColor(btRuA[muID - 1], colorSelect);
                        SetForeColor(btRuA[muID - 1], Color.Black);
                    }
                    else
                    {
                        SetColor(btRuA[muID - 1], colorError);
                        SetForeColor(btRuA[muID - 1], Color.White);
                    }
                }
                catch
                {
                }
            }
            else
            {	//RuB
                try
                {
                    if (tmpResult == 0)
                    {
                        SetColor(btRuB[muID - 1, ruID - 1], colorSelect);
                        SetForeColor(btRuB[muID - 1, ruID - 1], Color.Black);
                    }
                    else
                    {
                        SetColor(btRuB[muID - 1, ruID - 1], colorError);
                        SetForeColor(btRuB[muID - 1, ruID - 1], Color.White);
                    }
                }
                catch
                {
                }
            }

            //Error 상태 저장
            if (tmpResult == 0) Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0] = false;
            else
            {   //Error 발생
                if (!Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0])
                {   //정상에서 Error가 발생한 상태이므로 메세지창을 띄운다.
                    SetVisible(frmErrMsg, true);

                    if (ruID == 0)
                    {   //RU-A
                        frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 광중계장치(RU_A)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                            + Common.clsNMS.stationList[muID - 1] + " 광중계장치 상세 화면을\r\n확인하시기 바랍니다.");

                        AddStatus(Common.clsNMS.stationList[muID - 1] + " 광중계장치(RU_A)에 장애가 발생하였습니다.");
                    }
                    else
                    {   //RU-B
                        frmErrMsg.SetText(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(RU_B)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                            + Common.clsNMS.muruName[muID - 1].ruName[ruID].ruName + " 광중계장치 상세 화면을\r\n 확인하시기 바랍니다.");

                        AddStatus(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(RU_B)에 장애가 발생하였습니다.");
                    }
                }

                Common.clsNMS.flagRuBdaError[muID - 1, ruID, 0] = true;     //Error 발생
            }
        }

        private void nmsRuFmError_Display(byte muID, byte ruID)
        {   //전체화면에 주예비 상태 및 장애 표시
            /*
            //FM이 없으면 정상처리한다.
            if (ruID == 0)
            {
                if (!Common.clsNMS.muruFmExist[muID - 1].flagMu) return 0;
            }
            else
            {
                if (!Common.clsNMS.muruFmExist[muID - 1].flagRu[ruID - 1]) return 0;
            }
            */

            //FM의 장애 상태 분석
            int tmpResult =
                    muruNowData[muID].ruData[ruID].fmInfo.acInputFail +
                    muruNowData[muID].ruData[ruID].fmInfo.dcOutputFail +
                    muruNowData[muID].ruData[ruID].fmInfo.ldAlarm +
                    muruNowData[muID].ruData[ruID].fmInfo.pdAlarm;

            //전체화면의 RU버튼에 이상여부 표시
            if (ruID == 0)
            {	//RuA
                try
                {
                    if (tmpResult == 4)
                    {
                        SetColor(btRuAFm[muID - 1], colorSelect);
                        SetForeColor(btRuAFm[muID - 1], Color.Black);
                    }
                    else
                    {
                        SetColor(btRuAFm[muID - 1], colorError);
                        SetForeColor(btRuAFm[muID - 1], Color.White);
                    }
                }
                catch
                {
                }
            }
            else
            {	//RuB
                try
                {
                    if (tmpResult == 4)
                    {
                        SetColor(btRuBFm[muID - 1, ruID - 1], colorSelect);
                        SetForeColor(btRuBFm[muID - 1, ruID - 1], Color.Black);
                    }
                    else
                    {
                        SetColor(btRuBFm[muID - 1, ruID - 1], colorError);
                        SetForeColor(btRuBFm[muID - 1, ruID - 1], Color.White);
                    }
                }
                catch
                {
                }
            }

            //Error 상태 저장
            if (tmpResult == 4) Common.clsNMS.flagRuFmError[muID - 1, ruID, 0] = false;
            else
            {   //Error 발생
                if (!Common.clsNMS.flagRuFmError[muID - 1, ruID, 0])
                {   //정상에서 Error가 발생한 상태이므로 메세지창을 띄운다.
                    SetVisible(frmErrMsg, true);

                    if (ruID == 0)
                    {   //RU-A
                        frmErrMsg.SetText(Common.clsNMS.stationList[muID - 1] + " 광중계장치(FM)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                            + Common.clsNMS.stationList[muID - 1] + " 광중계장치 상세 화면을\r\n확인하시기 바랍니다.");

                        AddStatus(Common.clsNMS.stationList[muID - 1] + " 광중계장치(FM)에 장애가 발생하였습니다.");
                    }
                    else
                    {   //RU-B
                        frmErrMsg.SetText(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(FM)에 장애가 발생하였습니다.\r\n\r\n자세한 내용은 "
                            + Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치 상세 화면을\r\n확인하시기 바랍니다.");

                        AddStatus(Common.clsNMS.muruName[muID - 1].ruName[ruID - 1].ruName + " 광중계장치(FM)에 장애가 발생하였습니다.");
                    }
                }

                Common.clsNMS.flagRuFmError[muID - 1, ruID, 0] = true;     //Error 발생
            }
        }

        private void tmrMain_Tick(object sender, EventArgs e)
        {
            tmrMain.Stop();

            int i = 0, j = 0;

            switch (Common.clsNMS.nmsGUIUser)
            {
                case "분당선":
                case "경의일산선":
                case "수인선":
                    if (day != DateTime.Now.Day)
                    {   //날자가 변경되면..
                        day = DateTime.Now.Day;

                        //3. DB파일도 바꿔준다.
                        NMSDB_Connect();

                        //dTPStart.Value = DateTime.Now;
                        //dTPStop.Value = DateTime.Now;
                        //AddStatus("시간을 동기화 합니다.");
                    }
                    break;

                case "과천선":
                    //NMS Server와의 통신상태를 점검하기 위한 작업
                    for (i = 0; i < 2; i++)
                    {
                        if (cntServerPolling[i]++ > 5)
                        {
                            cntServerPolling[i] = 6;

                            if (flagServerPolling[i])
                            {   //NMS Server Stby과의 통신 이상 발생
                                flagServerPolling[i] = false;
                                SetColor(btConnectServer[i], Color.Red);

                                if (i == 0)
                                {
                                    AddStatus("NMS Server Main 통신이상 발생");
                                    JksSockMain.clsInit();
                                }
                                else
                                {
                                    AddStatus("NMS Server Stby 통신이상 발생");
                                    JksSockStby.clsInit();
                                }
                            }
                        }
                    }
                    break;
            }            

            //각 기지국과의 통신상태를 점검하기 위한 작업
            for (i = 0; i < Common.clsNMS.stationList.Count; i++)
            {
                //MU
                if (Common.clsNMS.muruComSt[i].cntMu++ > 30)
                {
                    Common.clsNMS.muruComSt[i].cntMu = 31;

                    if (Common.clsNMS.muruComSt[i].flagMu)
                    {
                        AddStatus(Common.clsNMS.stationList[i] + "(MU) 통신이상 발생");

                        Common.clsNMS.muruComSt[i].flagMu = false;

                        //현재 보고있는 MU화면이 있으면 통신불량 상태로 비활성화 시킨다.
                        if (i + 1 == Common.clsNMS.presentMUID) MuStInit(false);

                        try
                        {
                            SetColor(btMu[i], colorBase);
                            SetForeColor(btMu[i], Color.Black);
                        }
                        catch
                        {
                        }
                    }
                }

                //MU_FM
                if (Common.clsNMS.muruComSt[i].cntFm++ > 30)
                {
                    Common.clsNMS.muruComSt[i].cntFm = 31;

                    if (Common.clsNMS.muruComSt[i].flagFm)
                    {
                        //AddStatus(Common.clsNMS.stationList[i] + "(MU FM) 통신이상 발생");

                        Common.clsNMS.muruComSt[i].flagFm = false;

                        //현재 보고있는 MU화면이 있으면 통신불량 상태로 비활성화 시킨다.
                        if (i + 1 == Common.clsNMS.presentMUID) MuFmStInit(false);

                        try
                        {
                            SetColor(btMuFm[i], colorBase);
                            SetForeColor(btMuFm[i], Color.Black);
                        }
                        catch
                        {
                        }
                    }
                }

                //RuA
                if (Common.clsNMS.muruComSt[i].ruBdaCommSt[0].cntRu++ > 30)
                {
                    Common.clsNMS.muruComSt[i].ruBdaCommSt[0].cntRu = 31;

                    if (Common.clsNMS.muruComSt[i].ruBdaCommSt[0].flagRu)
                    {
                        AddStatus(Common.clsNMS.muruName[i].muName + "(RU_A) 통신이상 발생");

                        Common.clsNMS.muruComSt[i].ruBdaCommSt[0].flagRu = false;

                        //현재 보고있는 MU화면이 있으면 통신불량 상태로 비활성화 시킨다.
                        if ((i + 1 == Common.clsNMS.presentMUID) && (j + 1 == Common.clsNMS.presentRUID)) RuStInit(false);

                        try
                        {
                            SetColor(btRuA[i], colorBase);
                            SetForeColor(btRuA[i], Color.Black);
                        }
                        catch
                        {
                        }
                    }
                }

                //RuA_FM
                if (Common.clsNMS.muruComSt[i].ruBdaCommSt[0].cntFm++ > 30)
                {
                    Common.clsNMS.muruComSt[i].ruBdaCommSt[0].cntFm = 31;

                    if (Common.clsNMS.muruComSt[i].ruBdaCommSt[0].flagFm)
                    {
                        //AddStatus(Common.clsNMS.muruName[i].muName + "(RU_A FM) 통신이상 발생");

                        Common.clsNMS.muruComSt[i].ruBdaCommSt[0].flagFm = false;

                        //현재 보고있는 MU화면이 있으면 통신불량 상태로 비활성화 시킨다.
                        if ((i + 1 == Common.clsNMS.presentMUID) && (j + 1 == Common.clsNMS.presentRUID)) RuFmStInit(false);

                        try
                        {
                            SetColor(btRuAFm[i], colorBase);
                            SetForeColor(btRuAFm[i], Color.Black);
                        }
                        catch
                        {
                        }
                    }
                }

                //RuB
                for (j = 0; j < 4; j++)
                {
                    //RU
                    if (Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].cntRu++ > 30)
                    {
                        Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].cntRu = 31;

                        if (Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].flagRu)
                        {
                            if (Common.clsNMS.muruName[i].ruName[j].ruName != "")
                                AddStatus(Common.clsNMS.muruName[i].ruName[j].ruName + "(RU_B) 통신이상 발생");

                            Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].flagRu = false;

                            //현재 보고 있는 RU화면이 있으면 통신불량 상태로 비활성화 시킨다.
                            if ((i + 1 == Common.clsNMS.presentMUID) && (j + 1 == Common.clsNMS.presentRUID)) RuStInit(false);

                            try
                            {
                                SetColor(btRuB[i, j], colorBase);
                                SetForeColor(btRuB[i, j], Color.Black);
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                //RuB_FM
                for (j = 0; j < 4; j++)
                {
                    //RU
                    if (Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].cntFm++ > 30)
                    {
                        Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].cntFm = 31;

                        if (Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].flagFm)
                        {
                            //if (Common.clsNMS.muruName[i].ruName[j].ruName != "")
                            //    AddStatus(Common.clsNMS.muruName[i].ruName[j].ruName + "(RU_B FM) 통신이상 발생");

                            Common.clsNMS.muruComSt[i].ruBdaCommSt[j + 1].flagFm = false;

                            //현재 보고 있는 RU화면이 있으면 통신불량 상태로 비활성화 시킨다.
                            if ((i + 1 == Common.clsNMS.presentMUID) && (j + 1 == Common.clsNMS.presentRUID)) RuFmStInit(false);

                            try
                            {
                                SetColor(btRuBFm[i, j], colorBase);
                                SetForeColor(btRuBFm[i, j], Color.Black);
                            }
                            catch
                            {
                            }
                        }
                    }
                }

                //각 기지국의 주/예비/감청 통신 Error Check
                for (j = 0; j < 3; j++)
                {
                    if (commErrCheck[i].flagCommErr[j])
                    {   //주/예비/감청통신이 이상인 경우
                        try
                        {
                            commErrCheck[i].cntCommErr[j]++;
                        }
                        catch
                        {
                        }
                    }
                }
            }

            tmrMain.Start();
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            AddStatus("프로그램을 종료합니다.");
        }
    }
}
