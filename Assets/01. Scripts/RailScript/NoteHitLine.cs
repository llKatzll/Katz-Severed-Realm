using System;
using UnityEngine;

public class NoteHitLine : MonoBehaviour
{
    //레일 8개 지정한 키가 각 레일을 담당
    // ex) A - 1레일 S - 2레일 K - 3레일 L - 4레일 Q - 어퍼1레일 ......

    //키가 입력되었을 경우 판정선과 제일 가까운 노트를 판정해야함 (판정선을 기준으로)
    //판정 처리 (ms놀음)를 어떻게 처리해야할지가 관건

    //판정 세부<최고<일반<주의<피해(콤보브레이크)
    // Severance < Clean < Trace < Fracture < Ruin

    // Severance: |Δms| ≤ 35ms 세부
    // Clean: 35ms< |Δms| ≤ 75ms 퍼펙트
    // Trace: 75ms< |Δms| ≤ 120ms 그레잇
    // Fracture: 120ms< |Δms| ≤ 170ms 배드
    // Ruin: 170ms < |Δms| ≤ 220ms 콤보브레이크
    // Miss: |Δms| > 220ms 콤보브레이크, 점수없음

    // 히트 시 해당 판정에 대한 이펙트가 판정난 판정선에서 발동해야함.
    // 어퍼노트라면 어퍼노트의 이펙트를, 그라운드 노트라면 그라운드 노트의 이펙트를, 스플래시라면 스플래시 노트의 이펙트 요구

    // 판정날때마다 Debug로 해당 판정의 이름과 반응속도ms 출력.

    // 판정은 뭘로 해야하지? 노트와 판정선 둘다 Collider를 잡고 있어야하나? <- 노트에게 리지드바디 이식

    [SerializeField] private Note _note;

    public enum NoteType { Ground, Upper, Splash } //아직 스플래시 이펙트는 없음

    public enum NoteForm { Tap, HoldHead, HoldBody, HoldTail }


    [System.Serializable]
    public class NoteHitter
    {
        //노트의 타입
        //본래 노트판정선의 위치(이펙트 소환, 판정선등을 위해)
    }

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Hit();
        
    }

    void Hit()
    {
        //일단 임시로 해두나, 후에 플레이어가 직접 지정할 수 있도록 해야함.
        if (Input.GetKeyDown(KeyCode.A))
        {
            Debug.Log("A 눌림");
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            Debug.Log("S 눌림");
        }

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tap"))
            Debug.LogWarning("탭 태그됨");

        if (other.CompareTag("Upper"))
            Debug.LogWarning("어퍼 태그됨");

        if (other.CompareTag("Splash"))
            Debug.LogWarning("스플래시 태그됨");
    }
}
