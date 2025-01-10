using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PMGame;
using DG.Tweening;
using PMLib;


public class NavDistanceText : MonoBehaviour
{
    [SerializeField] MeshRenderer meshRender = null;
    [SerializeField] Transform obTransform = null;

    int m = 0;
    List<Vector2> offsetXY = new List<Vector2>(3000);
    Material material;
    int offsetIndex = 0;
    Comp_AutoPlay.eAutoMoveType moveType = Comp_AutoPlay.eAutoMoveType.NONE;

    public void Start()
    {
        obTransform.localScale = Vector3.one;
        material = meshRender.materials[0];
        SetOffsetList();
    }

    public void SetOffsetList()
    {
        float xStep = 1 / 16f;
        float yStep = 1 / 50f;
        int index = 0;

        offsetXY.Add(new Vector2(0, 0));

        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 50; y++)
            {
                float xPos = x * xStep;
                float yPos = y * yStep;

                if (index < 500)
                {
                    offsetXY.Add(new Vector2(xPos, yPos));
                    index++;
                }
                else if(index < 1000)
                {
                    for (int i = 0; i < 10; i++)
                    {
                        offsetXY.Add(new Vector2(xPos, yPos));
                        index++;
                    }
                }
                else if(index <= 3000)
                {
                    for (int i = 0; i < 100; i++)
                    {
                        offsetXY.Add(new Vector2(xPos, yPos));
                        index++;
                    }
                }
            }
        }
    }

    

    public void SetDistance(int remainDis, Comp_AutoPlay.eAutoMoveType autoMoveType = Comp_AutoPlay.eAutoMoveType.NONE)
    {
        if (offsetXY.Count ==0)
            return;
        if (remainDis > offsetXY.Count - 1)
            return;

        // 텍스트 컬러 셋팅
        if (moveType != autoMoveType)
        {
            moveType = autoMoveType;

            if (material != null)
            {
                if (moveType == Comp_AutoPlay.eAutoMoveType.QUEST_MAIN)
                    material.SetColor("_TintColor", PMCommon.TextColors.NAVSPLINE_BLUE_COLOR);
                else if (moveType == Comp_AutoPlay.eAutoMoveType.QUEST_SUB)
                    material.SetColor("_TintColor", PMCommon.TextColors.NAVSPLINE_ORANGE_COLOR);
                else if (moveType == Comp_AutoPlay.eAutoMoveType.COMMISSION)
                    material.SetColor("_TintColor", PMCommon.TextColors.NAVSPLINE_MAGENTA_COLOR);
                else
                    material.SetColor("_TintColor", PMCommon.TextColors.NAVSPLINE_GREEN_COLOR);
            }
        }

        if (remainDis == offsetIndex)
            return;

        //텍스트 애니메이션 셋팅
        obTransform.DOKill();
        obTransform.DOScale(Vector3.one * 1.2f, 0.1f)
                 .SetEase(Ease.OutQuad)
                 .OnComplete(() =>
                 {
                     obTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InQuad);
                 });
        offsetIndex = remainDis;

        //텍스트 셋팅
        material.mainTextureOffset = offsetXY[offsetIndex];
    }

    #region TestButton
    [Button("0")]
    private void ResetDistance()
    {
        m = 0;
        SetDistance(m);
    }
    [Button("+1")]
    private void PlusOne()
    {
        m++;
        SetDistance(m);
    }
    [Button("+10")]
    private void PlusTen()
    {
        m += 10;
        SetDistance(m);
    }
    [Button("+100")]
    private void PlusHun()
    {
        m += 100;
        SetDistance(m);
    }
    #endregion
}
