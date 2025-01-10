using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GDT;
using PMLib;
using CSShared;
using PMNet;


namespace PMGame
{
    public enum eResultGame
    {
        NONE,
        SUCCESS,
        TIMEOUT,
        LIFEOVER,
    }

    public class CardGamePopupDlg : UIBaseDialog
    {
        [SerializeField] private GameObject[] turnTextObj;
        [SerializeField] private GameObject[] levelLayout;

        [SerializeField] private SlicedFilledImage remainTimeBar;
        [SerializeField] private TextMeshProUGUI remainTimeText;

        [SerializeField] private RectTransform lifeRoot;
        [SerializeField] private Image lifeHeart;

        [SerializeField] private MiniGameCardSlot cardSlot;

#if !ART_WORK
        private List<MiniGameCardSlot> cardList = new List<MiniGameCardSlot>();
        private MiniGameLevelType curMiniGameLevel = MiniGameLevelType.MGT_EASY;
        private MiniGameListlType miniGame = MiniGameListlType.MLT_CARDGAME;
        private List<Image> lifeCount = new List<Image>();
        private List<int> selectCard = new List<int>(); //고른 카드

        private int remainLife; //남은 생명력
        public bool isMyturn = false;
        private bool isblockTurn = false; //연출 동안 카드 클릭 막기 
        private TimerDiff showFrontCardTimer = null;

        public override void _OpenDlg(object param = null)
        { 
            base._OpenDlg();
            isblockTurn = true;
            MyInfo_Couple myCouple = MyInfo.Get().GetCoupleInfo();
            if (myCouple == null)
                return;

            SetData();

            //백그라운드 예외처리
            //패킷이 왔을 때 UI가 열려있지 않은 상태에서 게임의 정상적인 진행을 위해 셋팅
            myCouple.GetCurGame(out List<int> _index, out int _remainHP, out bool _isRight);
            if (_index.Count != 0)
            {
                List<int> pickCard = _index;
                remainLife = _remainHP;
                isMyturn = true;

                for (int i = 0; i < cardList.Count; i++)
                {
                    cardList[i].SetCardState(eCardState.BACK);
                }

                SetReData(pickCard, 0, _isRight);

                //플레이중 셋팅
                myCouple.SetIsPlaying(true);
                isblockTurn = false;

                //타이머 보여주기
                remainTimeBar.fillAmount = 1;
                remainTimeText.text = GLStringUtil.GetFormat(GLTextManager.GetUIString("MAP_SEARCH_OPTION_SEC"), GLDataManager.Tables.GetCoupleGameList(miniGame).GameTime);
                //타이머 on
                CoupleManager.Instance.MiniGameStartTimer();
            }

            //게임을 껐다 켰을 때도 정상 진행....
            else if(param != null)
            {
                //재접속 시도 했을 때 진행중이던 게임 불러오기
                Game2Client.K_C_Notify_CoupleMiniGameState_S packet = param as Game2Client.K_C_Notify_CoupleMiniGameState_S;
                if (packet == null)
                    return;

                List<int> pickCard = packet.pickedCard;
                remainLife = packet.hp;
                isMyturn = packet.myTurn;
                SetReData(pickCard, packet.beginUnixTime);
            }

            //이게 정상 진행
            else
            {
                //타이머 보여주기
                remainTimeBar.fillAmount = 1;
                remainTimeText.text = GLStringUtil.GetFormat(GLTextManager.GetUIString("MAP_SEARCH_OPTION_SEC"), GLDataManager.Tables.GetCoupleGameList(miniGame).GameTime);

                //생명력 셋팅
                remainLife = GLDataManager.Tables.GetCoupleGameList(miniGame).MaxVitality;
                for (int i = 0; i < remainLife; i++)
                {
                    if (i >= lifeCount.Count)
                    {
                        var heart = Instantiate(lifeHeart.gameObject, lifeRoot);
                        lifeCount.Add(heart.GetComponent<Image>());
                    }
                    lifeCount[i].sprite = UIAtlasManager.Instance.GetSprite(eUIAtlas.UI_Ver_Sea_Atlas_Artwork, "Img_Couple_Heart");
                }
                GLUtil.SetActiveObject(lifeHeart, false);

                for (int i = 0; i < cardList.Count; i++)
                {
                    cardList[i].SetCardState(eCardState.BACK);
                }
                //연출 시작 전에 백그라운드로 나가면 앞장 안보여줌 : 게임 시작시간 + 연출시간 보다 현재 시간이 더 흘러있으면 스킵
                if (myCouple.GetStartTime() + GLDataManager.Tables.GDTConstantsTable.CoupleCardWaitTime >= GKClientSystem.Instance.CurrentUnixTime)
                    ShowFrontCard();
                else
                {
                    //플레이중 셋팅
                    myCouple.SetIsPlaying(true);
                    isblockTurn = false;

                    //타이머 on
                    CoupleManager.Instance.MiniGameStartTimer();
                }


                //카드 앞면 잠깐 보여주기
                void ShowFrontCard()
                {
                    MyInfo_Couple myCouple = MyInfo.Get().GetCoupleInfo();
                    if (myCouple == null)
                        return;

                    selectCard.Clear();

                    //카드 앞면 보여주기
                    for (int i = 0; i < cardList.Count; i++)
                    {
                        cardList[i].SetCardState(eCardState.FRONT);
                    }
                    
                    DateTime targetTime = GKClientSystem.Instance.CurrentLocalTime.AddSeconds(GLDataManager.Tables.GDTConstantsTable.CoupleCardWaitTime);

                    if (showFrontCardTimer == null)
                        showFrontCardTimer = TimerManager.Get().AddTargetTimeDiff(targetTime, TimerUpdate);

                    void TimerUpdate(TimerDiff timer)
                    {
                        if (timer.GetRemainTime() <= 0 || timer == null || timer.remove || timer.callBack == null)
                        {
                            for (int i = 0; i < cardList.Count; i++)
                            {
                                if (cardList[i].GetCardState() == eCardState.FRONT)
                                    cardList[i].SetCardState(eCardState.BACK);
                            }

                            //플레이중 셋팅
                            myCouple.SetIsPlaying(true);
                            isblockTurn = false;

                            //타이머 on
                            CoupleManager.Instance.MiniGameStartTimer();
                            return;
                        }
                    }
                }
            }
        }

        public void SetGameTimer(float remainsec)
        {
            MyInfo_Couple myCouple = MyInfo.Get().GetCoupleInfo();
            if (myCouple == null)
                return;

            if (myCouple.GetIsPlaying())
            {
                remainTimeBar.fillAmount = remainsec / GLDataManager.Tables.GetCoupleGameList(miniGame).GameTime;
                remainTimeText.text = GLStringUtil.GetFormat(GLTextManager.GetUIString("MAP_SEARCH_OPTION_SEC"), remainsec);
            }
        }
        public void SetData()
        {
            MyInfo_Couple myCouple = MyInfo.Get().GetCoupleInfo();
            if (myCouple == null)
                return;

            for(int i = 0; i < levelLayout.Length; i++)
            {
                GLUtil.SetActiveObject(levelLayout[i], false);
            }

            //레벨 설정, 레이아웃 켜기
            myCouple.GetMiniGameReady(out miniGame, out curMiniGameLevel);
            GLUtil.SetActiveObject(levelLayout[(int)curMiniGameLevel - 1], true);

            RectTransform parent = levelLayout[(int)curMiniGameLevel - 1].GetComponent<RectTransform>();
            List<CardMiniGameDataT> cardData = GLDataManager.Tables.GetCardMiniGameDataList(miniGame);

            //서버에서 받은 카드 리스트 가져오기
            List<int> cardIndex = myCouple.GetCardList();

            int pickCount = GLDataManager.Tables.GetMiniGameLevelList(miniGame).Find(x => x.GameLevel == curMiniGameLevel).PickUpCount;
            if (cardIndex.Count == 0 || cardIndex.Count < pickCount * 2 )
            {
                //카드 데이터가 이상하게 왔음!
                GLDebug.LogError("No CardData : " + cardIndex.Count);
                _CloseDlg();
                return;
            }

            for (int i = 0; i < cardIndex.Count; i++)
            {
                MiniGameCardSlot slot = Instantiate(cardSlot, parent);
                cardList.Add(slot);
                cardList[i].SetCardData(cardData[cardIndex[i] - 1].Icon, i, CardSelectTurn);
                GLUtil.SetActiveObject(cardList[i], true);
            }
            //선턴?
            isMyturn = myCouple.GetisFirstTurn();
            SetMyTurn();
        } 

        //재접속시 현재 게임 진행 상황 셋팅 (백그라운드 싱크 추가)
        public void SetReData(List<int> pickedCard, int startTime = 0, bool isRight = true)
        {
            MyInfo_Couple myCouple = MyInfo.Get().GetCoupleInfo();
            if (myCouple == null)
                return;

            SetMyTurn();

            if(isRight)
            {
                for (int i = 0; i < pickedCard.Count; i++)
                {
                    if (cardList.Count < pickedCard[i])   //잘못된 데이터
                        continue;
                    cardList[pickedCard[i]].SetCardState(eCardState.FRONT);
                }
            }

            if(startTime != 0)
            {
                float remainsec = GLDataManager.Tables.GetCoupleGameList(miniGame).GameTime - (GKClientSystem.Instance.CurrentUnixTime - startTime);
                remainTimeBar.fillAmount = remainsec / GLDataManager.Tables.GetCoupleGameList(miniGame).GameTime;
                remainTimeText.text = GLStringUtil.GetFormat(GLTextManager.GetUIString("MAP_SEARCH_OPTION_SEC"), remainsec);
                isblockTurn = false;
            }            

            //생명력 진행상황 셋팅
            for (int i = 0; i < GLDataManager.Tables.GetCoupleGameList(miniGame).MaxVitality; i++)
            {
                var heart = Instantiate(lifeHeart.gameObject, lifeRoot);
                lifeCount.Add(heart.GetComponent<Image>());
                if(i < remainLife)
                    lifeCount[i].sprite = UIAtlasManager.Instance.GetSprite(eUIAtlas.UI_Ver_Sea_Atlas_Artwork, "Img_Couple_Heart");
                else
                    lifeCount[i].sprite = UIAtlasManager.Instance.GetSprite(eUIAtlas.UI_Ver_Sea_Atlas_Artwork, "Img_Empty_Heart");
            }
            GLUtil.SetActiveObject(lifeHeart, false);

            myCouple.ResetCurGameData();
        }
        //카드 선택 뒤집기 
        public void CardSelectTurn(int index)
        {
            if(isblockTurn) return;

            //내 차례 아닌데 카드 고르면 안됨!
            if (!isMyturn) return;

            //선택 되어있는 카드가 있을 땐 터치 막기
            if (selectCard.Count == 2) return;

            //같은 index 중복 클릭 안됨
            if (selectCard.Contains(index)) return;


            if (cardList[index].GetCardState() == eCardState.BACK)
            {
                cardList[index].SetCardState(eCardState.SELECT);
                selectCard.Add(index);

                if (selectCard.Count == 2)
                {
                    isblockTurn = true;
                    SendPacket.Send_ReqCooupleMiniGame_PickCard(selectCard);
                }
            }
        }

        IEnumerator SelectCardsReceiveC(List<int> index, bool isCorrect)
        {
            yield return new WaitForSeconds(1f);

            //한 턴에 한 쌍만 뒤집을 수 있음             
            if (index.Count != 2)
                yield break;

            //짝이 맞았다
            if (isCorrect)
            {
                cardList[index[0]].SetCardState(eCardState.FRONT);
                cardList[index[1]].SetCardState(eCardState.FRONT);
            }
            //틀렸다
            else
            {
                cardList[index[0]].SetCardState(eCardState.BACK);
                cardList[index[1]].SetCardState(eCardState.BACK);
            }

            selectCard.Clear();
            isblockTurn = false;
            yield return null;
        }

        //고른 카드에 대한 응답
        //index = 뒤집은 카드 번호
        //cards = 카드 종류
        public void SelectCardsReceive(List<int> index, List<int> cards, int HP, bool isCorrect)
        {
            isblockTurn = true;
            //Noti 일 때 상대가 뒤집은 카드 확인 시켜주기
            if (isMyturn == false)
            {
                cardList[index[0]].SetCardState(eCardState.SELECT);
                cardList[index[1]].SetCardState(eCardState.SELECT);
            }

            if(!isCorrect)
            {
                SetRemainHP(HP);
            }
            StartCoroutine(SelectCardsReceiveC(index, isCorrect));

        }

        public void SetRemainHP(int remainHP)
        {
            if (lifeCount.Count <= remainHP)
                return;
            lifeCount[remainHP].sprite = UIAtlasManager.Instance.GetSprite(eUIAtlas.UI_Ver_Sea_Atlas_Artwork, "Img_Empty_Heart");
        }

        public void SetMyTurn()
        {
            GLUtil.SetActiveObject(turnTextObj[0], isMyturn);
            GLUtil.SetActiveObject(turnTextObj[1], !isMyturn);
        }

        public void EndGame(eResultGame result)
        {
            UIManager.Instance.OpenDlgAsync(eUIDialogID.UID_COUPLE_MINIGAME_RESULT_POPUP, result);
        }

        public override void _CloseDlg()
        {
            foreach (Transform child in levelLayout[(int)curMiniGameLevel - 1].transform)
            {
                Destroy(child.gameObject);
            }

            cardList.Clear();
            selectCard.Clear();
            showFrontCardTimer = null;
            base._CloseDlg();
            CoupleManager.Instance.EndGameTimer();
        }

        public override void OnClickClose()
        {
            return;
        }

#endif
    }
}
