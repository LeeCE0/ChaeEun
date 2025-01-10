using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using PMLib;
using PMCommon;
using UnityEngine.AI;
using Unity.Mathematics;

public class NavSpline : MonoBehaviour
{
    [SerializeField] SplineContainer container;

    [SerializeField] SplineExtrude controller;

    [SerializeField] SplineAnimate anim;

    [Header("기준 Y축")]
    [SerializeField] float defaultY = 1f;

    [Header("이펙트 생성 최소 거리")]
    [SerializeField] float minActiveDistance = 15f;

    [Header("랜덤 노드 최소 거리")]
    [SerializeField] float midNodeDistance = 5f;

    [Header("중간 노드 X 랜덤 범위")]
    [SerializeField] float randomMinX = -1f;
    [SerializeField] float randomMaxX = 1f;

    [Header("중간 노드 Y 랜덤 범위")]
    [SerializeField] float randomMinY = -1f;
    [SerializeField] float randomMaxY = 1f;

    [Header("라인 지름")]
    [SerializeField] float radius = 0.05f;

#if UNITY_EDITOR

    private class debugKnot
    {
        public Vector3 position;
        public bool isOriginal;
    }

    private List<debugKnot> debugKnots = new List<debugKnot>();
#endif

    public void SetSpline(Vector3[] path)
    {
        if (container.Spline == null)
            return;
        if (container.Spline.Count > 0)
            return;

#if UNITY_EDITOR
        debugKnots.Clear();
#endif

        controller.Radius = radius;

        for (int i = 0; i < path.Length; i++)
        {
            //오리지널 노드도 Y 랜덤값
            path[i].y += defaultY + UnityEngine.Random.Range(randomMinY, randomMaxY) / 2;

            container.Spline.Add(new BezierKnot(path[i]));

#if UNITY_EDITOR
            debugKnot temp = new debugKnot();
            temp.position = new Vector3(path[i].x, path[i].y, path[i].z);
            temp.isOriginal = true;
            debugKnots.Add(temp);
#endif

            if (i < path.Length - 1)
                SetCustomeNode(path[i], path[i + 1]);
        }
        container.Spline.SetTangentMode(TangentMode.AutoSmooth);
        controller.Rebuild();
    }

    public void SetCustomeNode(Vector3 startNode, Vector3 lastNode)
    {
        Vector3 midnode = new Vector3();

        if (Vector3.Distance(startNode, lastNode) > midNodeDistance)
        {
            float randomX = 0f;
            float randomY = 0f;
            //생성 할 노드 수
            int cnt = (int)(Vector3.Distance(startNode, lastNode) / midNodeDistance) - 1;

            Vector3 step = (lastNode - startNode) / (cnt + 1);
            for (int i = 0; i < cnt; i++)
            {
                midnode = startNode + step * (i + 1);

                //전 노드의 랜덤값 반전
                int x = 0;
                int y = 0;

                if (randomX > 0) x = -1;
                else x = 1;

                if (randomY > 0) y = -1;
                else y = 1;

                //노드와 땅의 기준점 보정
                Vector3 knot = ControllYtoRay(midnode);

                //노드의 랜덤값을 저장
                randomX = UnityEngine.Random.Range(randomMinX, randomMaxX);
                randomY = UnityEngine.Random.Range(randomMinY, randomMaxY);

                knot.x += randomX * x;
                knot.y += randomY * y;

                container.Spline.Add(new BezierKnot(knot));
#if UNITY_EDITOR
                debugKnot temp = new debugKnot();
                temp.position = new Vector3(knot.x, knot.y, knot.z);
                temp.isOriginal = false;

                debugKnots.Add(temp);
#endif
            }
        }
    }
    public void ClearNode()
    {
        container.Spline.Clear();
    }

    public bool IsMinDistance(Vector3 startNode, Vector3 lastNode)
    {
        return Vector3.Distance(lastNode, startNode) >= minActiveDistance;
    }

    //커스텀 노드가 땅 아래로 파묻히지 않도록 Y보정, 노드를 넣고 보정만 해서 뱉기
    public Vector3 ControllYtoRay(Vector3 node)
    {
        Ray ray = new Ray(node, Vector3.down);
        RaycastHit hit;
        int layer = 0;
        layer |= 1 << CharDefine.Layer_Map;
        float valueY = 0f;

        if (Physics.Raycast(ray, out hit, 1000, layer))
        {
            valueY = hit.point.y;
        }

        //땅에 부딪쳤다면 Y좌표 가져와서 더해주기
        if (valueY != 0)
        {
            node.y = valueY + defaultY;
        }

        return node;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        var _pos = this.transform.position;

        if (debugKnots == null)
            return;
        if (debugKnots.Count == 0)
            return;

        var _color = Gizmos.color;

        foreach (var knot in debugKnots)
        {
            if (knot.isOriginal == true)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(knot.position, 0.25f);
            }
            else
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(knot.position, 0.2f);
            }
        }

        Gizmos.color = _color;
    }
#endif
}
