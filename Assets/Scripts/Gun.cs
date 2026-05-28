using System.Collections;
using UnityEngine;

// 총을 구현
public class Gun : MonoBehaviour
{
    // 총의 상태를 표현하는 데 사용할 타입을 선언
    public enum State
    {
        Ready, // 발사 준비됨
        Empty, // 탄알집이 빔
        Reloading // 재장전 중
    }

    public State state { get; private set; } // 현재 총의 상태

    public Transform fireTransform; // 탄알이 발사될 위치

    public ParticleSystem muzzleFlashEffect; // 총구 화염 효과
    public ParticleSystem shellEjectEffect; // 탄피 배출 효과

    private LineRenderer bulletLineRenderer; // 탄알 궤적을 그리기 위한 렌더러

    private AudioSource gunAudioPlayer; // 총 소리 재생기

    public GunData gunData; // 총의 현재 데이터

    private float fireDistance = 50f; // 사정거리

    public int ammoRemain = 100; // 남은 전체 탄알
    public int magAmmo; // 현재 탄알집에 남아 있는 탄알

    private float lastFireTime; // 총을 마지막으로 발사한 시점

    private void Awake()
    {
        // 사용할 컴포넌트의 참조 가져오기
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        //사용할 점을 두 개로 변경
        bulletLineRenderer.positionCount = 2;   //라인렌더러를 그릴 시작점과 끝점 2개로 설정
        //라인 렌더러를 비활성화
        bulletLineRenderer.enabled = false; //처음에는 라인렌더러를 비활성화하여 보이지 않도록 설정
    }

    private void OnEnable()
    {
        // 총 상태 초기화
        ammoRemain = gunData.startAmmoRemain;   //시작할 때 남은 전체 탄알 수 설정(100발)
        //현재 탄창을 가득 채우기
        magAmmo = gunData.magCapacity;  //시작할 때 탄알집을 가득 채움(25발)

        //총의 현재 상태를 총을 쏠 준비가 된 상태로 변경
        state = State.Ready;    //총의 상태를 발사 준비된 상태로 설정
        //마지막으로 총을 쏜 시점을 초기화
        lastFireTime = 0;   //총을 마지막으로 발사한 시점을 초기화
    }

    // 발사 시도
    public void Fire()
    {
        //현재 상태가 발사 가능한 상태이고,
        //마지막 총 발사 시점에서 timeBetFire만큼 시간이 지났는지 확인
        if (state == State.Ready && Time.time >= lastFireTime + gunData.timeBetFire)
        {
            lastFireTime = Time.time;   //마지막 총 발사 시점 갱신
            Shot(); //실제 발사 처리 실행
        }
    }

    // 실제 발사 처리
    private void Shot()
    {
        //레이캐스트에 의한 충돌 정보를 저장하는 컨테이너
        RaycastHit hit;
        //탄알이 맞은 곳을 저장할 변수
        Vector3 hitPosition = Vector3.zero;

        //레이캐스트(시작 지점, 방향, 충돌 정보 컨테이너, 사정거리)
        //레이를 쏠 위치, 레이의 방향, 레이가 무언가에 맞았을 때의 정보를 저장할 변수, 레이의 최대 사정거리
        if (Physics.Raycast(fireTransform.position, fireTransform.forward, out hit, fireDistance))
        {
            //레이캐스트가 무언가에 맞았을 때
            //충돌한 상대방으로부터 IDamageable 오브젝트를 가져오기 시도
            //다형성: 맞은 대상이 IDamageable을 구현하고 있다면 데미지를 주기 위해 인터페이스 타입으로 참조
            IDamageable target = hit.collider.GetComponent<IDamageable>();

            //상대방으로부터 IDamageable 오브젝트를 가져오는 데 성공했다면
            if (target != null)
            {
                //상대방의 OnDamage함수를 실행시켜 상대방에게 데미지 주기
                target.OnDamage(gunData.damage, hit.point, hit.normal);
            }

            hitPosition = hit.point;    
        }
        else
        {
            //레이캐스트가 아무것도 맞지 않았을 때
            //라인렌더러를 그릴 끝점을 저장(사거리 50m)
            hitPosition = fireTransform.position + fireTransform.forward * fireDistance;
        }
        //발사 이펙트 재생 시작
        StartCoroutine(ShotEffect(hitPosition));

        //남은 탄알 수를 -1
        magAmmo--;
        if (magAmmo <= 0)   //탄알집에 탄알이 다 떨어졌다면
        {
            state = State.Empty;    //총의 상태를 Empty로 변경
        }
    }

    // 발사 이펙트와 소리를 재생하고 탄알 궤적을 그림
    private IEnumerator ShotEffect(Vector3 hitPosition)
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        gunAudioPlayer.PlayOneShot(gunData.shotClip);

        bulletLineRenderer.SetPosition(0, fireTransform.position);
        bulletLineRenderer.SetPosition(1, hitPosition);
        // 라인 렌더러를 활성화하여 탄알 궤적을 그림
        bulletLineRenderer.enabled = true;

        // 0.03초 동안 잠시 처리를 대기(
        yield return new WaitForSeconds(0.03f);

        // 라인 렌더러를 비활성화하여 탄알 궤적을 지움
        bulletLineRenderer.enabled = false;
    }

    // 재장전 시도
    public bool Reload()
    {
        //이미 재장전 중이거나 남은 탄알이 없거나 탄창에 탄알이 가득한 경우 재장전할수 없음
        if (state == State.Reloading || ammoRemain <= 0 || magAmmo >= gunData.magCapacity)
        {
            return false;
        }
        StartCoroutine(ReloadRoutine());
        return true;

    }

    // 실제 재장전 처리를 진행
    private IEnumerator ReloadRoutine()
    {
        // 현재 상태를 재장전 중 상태로 전환
        state = State.Reloading;

        gunAudioPlayer.PlayOneShot(gunData.reloadClip);

        // 재장전 소요 시간 만큼 처리 쉬기
        yield return new WaitForSeconds(gunData.reloadTime);    //1.8초동안 재장전 시간 필요함

        int ammoToFill = gunData.magCapacity - magAmmo; //탄알집을 채우기 위해 필요한 탄알 수

        if (ammoRemain < ammoToFill)    
        {
            ammoToFill = ammoRemain;    //남은 탄알이 필요한 탄알보다 적다면 남은 탄알만 채움
        }

        magAmmo += ammoToFill;  //탄알집에 탄알 채우기
        ammoRemain -= ammoToFill;   //남은 탄알에서 채운 탄알만큼 빼기

        // 총의 현재 상태를 발사 준비된 상태로 변경
        state = State.Ready;
    }
}