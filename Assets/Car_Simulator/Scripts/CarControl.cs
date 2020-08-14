using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControl : MonoBehaviour
{
    [Header("Info")]
    public WheelControlInfo wheelinfo;
    public AudioControlInfo audioinfo;
    public EffectControlInfo effectinfo;

    [Header("Component")]
    public Rigidbody rigid;

    CameraType cameraType = CameraType.thirdPerson;
    SoundType soundSelected;

    GameObject[] driftEffectClone;

    bool isStart;
    bool isSetting;
    bool isInstantiated;

    void Awake()
    {
        driftEffectClone = new GameObject[wheelinfo.AllWheel.Length];
        WheelCenterSetting();
    }

    void FixedUpdate()
    {
        SwitchCameraView();
        CameraEffect(cameraType);

        if (isStart)
        {
            WheelControl();
            DriftControl();
            LightControl();
            CarSoundEffect(audioinfo.audio[0], SoundType.EngineAccele);
        }
        else
        {
            StartCoroutine(Start());

            foreach (WheelCollider wheel in wheelinfo.AllWheel)
            {
                UpdateWheelVisual(wheel.transform.GetChild(0), wheel);
            }
        }
    }

    //휠 센터 좌표 설정
    void WheelCenterSetting()
    {
        foreach(WheelCollider wheel in wheelinfo.AllWheel)
        {
            wheel.center = wheel.transform.GetChild(0).transform.localPosition;
        }
    }

    //휠 제어 함수
    void WheelControl()
    {
        //휠 각도
        float rot = wheelinfo.SteerRot * Input.GetAxis("Horizontal");
        //휠 토크
        float Torque = wheelinfo.MotorTorque * Input.GetAxis("Vertical");
        //브레이크 토크 수동
        float Brake = wheelinfo.BrakeTorque * Input.GetAxis("Brake");

        //방향 담당 휠 제어
        foreach (WheelCollider wheel in wheelinfo.SteerWheel)
        {
            wheel.steerAngle = rot;

            UpdateWheelVisual(wheel.transform.GetChild(0), wheel);
        }
        //토크 담당 휠 제어
        foreach (WheelCollider wheel in wheelinfo.MotorWheel)
        {
            wheel.motorTorque = Torque;
            wheel.brakeTorque = Brake;

            UpdateWheelVisual(wheel.transform.GetChild(0), wheel);
        }
    }

    //드리프트 제어 함수
    void DriftControl()
    {
        float FrictionAverage = 0f;
        float DriftForceF = (Input.GetAxis("Drift") * wheelinfo.driftF_Forward * Input.GetAxis("Vertical")) / rigid.mass;

        rigid.velocity += transform.forward * DriftForceF;

        foreach (WheelCollider wheel in wheelinfo.AllWheel)
        {
            WheelFrictionCurve wheelCurveForward;
            WheelFrictionCurve wheelCurveSide;

            wheelCurveForward = wheel.forwardFriction;
            wheelCurveSide = wheel.sidewaysFriction;

            wheelCurveForward.extremumValue = Input.GetButton("Drift") ? Mathf.Clamp((wheelinfo.frictionStanForward / (1 + Input.GetAxis("Drift"))) / (1 + rigid.velocity.magnitude), 0.35f, 3) : 2;
            wheelCurveForward.asymptoteValue = Input.GetButton("Drift") ? Mathf.Clamp((wheelinfo.frictionStanForward / (1 + Input.GetAxis("Drift"))) / (1 + rigid.velocity.magnitude), 0.35f, 3) : 2;
            wheelCurveSide.extremumValue = Input.GetButton("Drift") ? Mathf.Clamp((wheelinfo.frictionStanSide / (1 + Input.GetAxis("Drift") * (1 + Input.GetAxis("Horizontal")))) / (1 + rigid.velocity.magnitude), 0.4f, 3) : 2;
            wheelCurveSide.asymptoteValue = Input.GetButton("Drift") ? Mathf.Clamp((wheelinfo.frictionStanSide / (1 + Input.GetAxis("Drift") * (1 + Input.GetAxis("Horizontal")))) / (1 + rigid.velocity.magnitude), 0.4f, 3) : 2;

            wheel.forwardFriction = wheelCurveForward;
            wheel.sidewaysFriction = wheelCurveSide;

            FrictionAverage += (wheelCurveForward.extremumValue + wheelCurveSide.extremumValue) / 2;
        }

        FrictionAverage = FrictionAverage / wheelinfo.AllWheel.Length;

        if (FrictionAverage < wheelinfo.whenDrift)
        {
            CarSoundEffect(audioinfo.audio[1], SoundType.Drift);
        }

        CarDrivingEffect(FrictionAverage);
    }

    //휠 운동 시각화
    void UpdateWheelVisual(Transform trans, WheelCollider wheelCol)
    {
        Vector3 UpdatePos;
        Quaternion UpdateRot;

        //휠 운동 연산 결과를 월드 좌표로 변환
        wheelCol.GetWorldPose(out UpdatePos, out UpdateRot);

        trans.position = UpdatePos;
        trans.rotation = UpdateRot;
    }

    //카메라 효과
    void CameraEffect(CameraType cameraType)
    {
        Camera.main.fieldOfView = 60 + Mathf.Clamp(rigid.velocity.magnitude * 3, 0, 100);

        switch (cameraType)
        {
            case CameraType.thirdPerson:

                Camera.main.transform.localPosition = new Vector3(Mathf.Clamp(Input.GetAxis("Horizontal") * rigid.velocity.magnitude * 0.01f, -5, 5), effectinfo.thirdPersonCamera.localPosition.y, effectinfo.thirdPersonCamera.localPosition.z);
                Camera.main.transform.rotation = effectinfo.thirdPersonCamera.rotation;

                foreach (GameObject mirror in effectinfo.Mirrors)
                {
                    mirror.SetActive(false);
                }

                break;

            case CameraType.firstPerson:

                Camera.main.transform.localPosition = effectinfo.firstPersonCamera.localPosition;
                Camera.main.transform.rotation = effectinfo.firstPersonCamera.rotation;

                foreach (GameObject mirror in effectinfo.Mirrors)
                {
                    mirror.SetActive(true);
                }

                break;
        }
    }

    void SwitchCameraView()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            switch (cameraType)
            {
                case CameraType.firstPerson:

                    cameraType = CameraType.thirdPerson;

                    break;

                case CameraType.thirdPerson:

                    cameraType = CameraType.firstPerson;

                    break;
            }
        }
    }

    //소리 효과 제어
    void CarSoundEffect(AudioSource audio, SoundType type)
    {
        audio.clip = audioinfo.audioClip[(int)type];

        switch (type)
        {
            case SoundType.EngineStart:

                audio.volume = 1;
                audio.loop = false;
                
                audio.Play();

                break;

            case SoundType.EngineAccele:

                audio.volume = 1;
                audio.loop = true;

                if (soundSelected != SoundType.EngineAccele)
                    audio.Play();

                audio.pitch = Mathf.Clamp(0.5f + wheelinfo.MotorTorque * Input.GetAxis("Vertical") * 0.01f, 0, 1.8f);

                break;

            case SoundType.Drift:

                audio.volume = 1;
                audio.loop = false;

                if (!audio.isPlaying)
                    audio.Play();

                break;
        }

        soundSelected = type;
    }

    //이동 효과 제어
    void CarDrivingEffect(float friction)
    {
        TrailRenderer[] trails = new TrailRenderer[wheelinfo.AllWheel.Length];
        
        if (!isInstantiated)
        {
            for (int i = 0; i < wheelinfo.AllWheel.Length; i++)
            {
                driftEffectClone[i] = Instantiate(effectinfo.driftTrailObj, wheelinfo.AllWheel[i].transform.position + new Vector3(0, -0.25f, 0), Quaternion.Euler(90, 0, 0));
            }

            isInstantiated = true;
        }

        for (int i = 0; i < wheelinfo.AllWheel.Length; i++)
        {
            trails[i] = driftEffectClone[i].GetComponent<TrailRenderer>();
            trails[i].emitting = friction < wheelinfo.whenDrift && wheelinfo.AllWheel[i].isGrounded ? true : false;

            driftEffectClone[i].transform.position = wheelinfo.AllWheel[i].transform.GetChild(0).transform.position + new Vector3(0, -0.25f, 0);
        }
    }

    void LightControl()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            foreach (Light light in effectinfo.frontLight)
            {
                light.enabled = light.enabled ? false : true;
            }
        }
        else if (Input.GetAxis("Vertical") < 0 || Input.GetButton("Brake"))
        {
            foreach (Light light in effectinfo.backLight)
            {
                light.enabled = true;
            }

            effectinfo.backLightMesh.material = effectinfo.backLightOnMaterial;
        }
        else
        {
            foreach (Light light in effectinfo.backLight)
            {
                light.enabled = false;
            }

            effectinfo.backLightMesh.material = effectinfo.backLightOffMaterial;
        }
    }

    //시동 제어
    IEnumerator Start()
    {
        if (!isSetting && Input.GetButtonDown("CarStart"))
        {
            isSetting = true;

            CarSoundEffect(audioinfo.audio[0], SoundType.EngineStart);
            yield return new WaitForSeconds(2);
            isStart = true;
        }
    }
}

//휠 제어 정보
[System.Serializable]
public struct WheelControlInfo
{
    public float MotorTorque;
    public float BrakeTorque;
    public float BrakeRateTIme;
    public float SteerRot;

    [Space(10f)]
    public float frictionStanForward;
    public float frictionStanSide;
    public float whenDrift;

    [Space(10f)]
    public float driftF_Forward;

    [Space(10f)]
    public WheelCollider[] SteerWheel;
    public WheelCollider[] MotorWheel;
    public WheelCollider[] AllWheel;
}

//오디오 제어 정보
[System.Serializable]
public struct AudioControlInfo
{
    public AudioSource[] audio;
    public AudioClip[] audioClip;
}

//이펙트 제어 정보
[System.Serializable]
public struct EffectControlInfo
{
    [Header("Obj")]
    public GameObject driftTrailObj;

    [Header("CameraView")]
    public Transform firstPersonCamera;
    public Transform thirdPersonCamera;

    [Header("Light")]
    public Light[] frontLight;
    public Light[] backLight;

    public MeshRenderer backLightMesh;
    public Material backLightOnMaterial;
    public Material backLightOffMaterial;

    [Header("Mirror")]
    public GameObject[] Mirrors;
}

//카메라 시점 타입
public enum CameraType
{
    thirdPerson,
    firstPerson
}

//음향 효과 타입
public enum SoundType
{
    EngineStart,
    EngineAccele,
    Drift
}
