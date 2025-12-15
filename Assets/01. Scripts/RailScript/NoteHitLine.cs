using System;
using UnityEngine;

public class NoteHitLine : MonoBehaviour
{

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
