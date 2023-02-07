using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class p2p : MonoBehaviour
{
    //Mic select...
    //[SerializeField] TMP_Dropdown dropdown;
    //[SerializeField] TextMeshProUGUI selected_mic_text_info;

    //public static string selected_micreophone_name = null;
    //public static int selected_microphone_index;
    
    private string microphone = null;
    private List<IEnumerator> threadPumpList = new List<IEnumerator>();
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioSource outputAudioSource;

    //[SerializeField] private Button createOffer_btn;
    //[SerializeField] private Button createAnswer_btn;
    //[SerializeField] private Button addAnswer_btn;
    [SerializeField] private TMP_InputField offer_textbox;
    [SerializeField] private TMP_InputField answer_textbox;


    private RTCPeerConnection _pc1,_pc2;
    private MediaStream localStream;
    private MediaStream remoteStream;
    

    private RTCSessionDescriptionAsyncOperation offer;

    private AudioClip m_clipInput;
    private AudioStreamTrack m_audioTrack;

    int m_samplingFrequency = 48000;
    int m_lengthSeconds = 1;
    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };
        return config;
    }
    void SetupMicro()
    {
        microphone = Microphone.devices[0];
        Debug.Log(microphone);

        StartCoroutine(WebRTC.Update());
        m_clipInput = Microphone.Start(microphone, true, m_lengthSeconds, m_samplingFrequency);

        inputAudioSource.loop = true;
        inputAudioSource.clip = m_clipInput;
    }
    // Start is called before the first frame update
    void Start()
    {
        SetupMicro();

        m_audioTrack = new AudioStreamTrack(inputAudioSource);
        m_audioTrack.Loopback = true;

        remoteStream = new MediaStream();

        remoteStream.OnAddTrack += OnAddTrack;

        var conf = GetSelectedSdpSemantics();

        _pc1 = new RTCPeerConnection();
        _pc2 = new RTCPeerConnection();



        m_audioTrack = new AudioStreamTrack(inputAudioSource);

        AddIce1();
        AddIce2();


        
        _pc1.OnIceCandidate = candidate => 
        {
            var op = _pc2.AddIceCandidate(candidate);
            if(op)
            {
                Debug.Log("<color=pink> Added candidate to pc1: </color>" + candidate.Candidate);
            }
        };
        _pc2.OnIceCandidate = candidate => 
        {
            var op = _pc1.AddIceCandidate(candidate);
            if(op)
            {
                
                Debug.Log("<color=pink> Added candidate to pc2: </color>" + candidate.Candidate);
            }
        };
        #region calback
        _pc1.OnConnectionStateChange = e => Debug.Log("<color=red> OnConnectionStateChange</color> <color=magenta> 1</color> : " + e);
        _pc1.OnIceConnectionChange = e => Debug.Log("<color=green>OnIceConnectionChange</color> <color=magenta> 1</color> : " + e);
        _pc1.OnIceGatheringStateChange= e => Debug.Log("<color=cyan>OnIceGatheringStateChange</color><color=magenta> 1</color> : " + e); _pc1.OnDataChannel = channel => Debug.Log("data channel create 1");
        _pc1.OnDataChannel = channel => Debug.Log("data channel create 1");
        

        _pc2.OnConnectionStateChange = e => Debug.Log("<color=red> OnConnectionStateChange</color> <color=yellow> 2</color> : " + e);
        _pc2.OnIceConnectionChange = e => Debug.Log("<color=green>OnIceConnectionChange</color> <color=yellow> 2</color> : " + e);
        _pc2.OnIceGatheringStateChange= e => Debug.Log("<color=cyan>OnIceGatheringStateChange</color><color=yellow> 2</color> : " + e);
        _pc2.OnDataChannel = channel => Debug.Log("data channel create 2");
        #endregion
        
        _pc1.OnNegotiationNeeded = () => Debug.Log("NEED 1");
        _pc2.OnNegotiationNeeded = () =>  Debug.Log("NEED 2");
        
        _pc2.OnTrack = e => 
        {
            Debug.Log("on track");
            remoteStream.AddTrack(e.Track);
        };

        _pc2.OnTrack = (RTCTrackEvent e) => handleRemoteTrack(e);    
        

    }
    public void handleRemoteTrack(RTCTrackEvent e)
    {
        Debug.Log("HandleRemoteTrack");
        if (e.Track is AudioStreamTrack)
        {
            remoteStream.AddTrack(e.Track);
        }
    }
    void OnAddTrack(MediaStreamTrackEvent e)
    {
        var track = e.Track as AudioStreamTrack;
        outputAudioSource.SetTrack(track);
        outputAudioSource.loop = true;
        outputAudioSource.Play();

    }
    #region AddIce
    private void AddIce1()
    {
        _pc1.AddTrack(m_audioTrack, localStream);
        var transceiver1 = _pc1.GetTransceivers().First();
    }
    private void AddIce2()
    {
        var transceiver2 = _pc2.AddTransceiver(TrackKind.Audio);
        transceiver2.Direction = RTCRtpTransceiverDirection.RecvOnly;
    }
    private void AddIce3()
    {
        _pc1.RestartIce();
        _pc2.RestartIce();   
        Debug.Log("restart");
    }
    #endregion
    #region addAns
    private void SetRemote1()
    {
        /*RTCSessionDescription desc = new RTCSessionDescription();
        var answer = answer_textbox.text;
        desc.sdp = answer;
        var op=_pc1.SetRemoteDescription(ref desc);
        */
        var op=_pc1.SetRemoteDescription(ref ansDesc);//ERROR IF FROM TEXT BOX chaned from desc
        if (!op.IsError)
        {
            OnSetRemoteSuccess();
        }
    }
    #endregion
    #region createAns
    RTCSessionDescription offerDesc;
    RTCSessionDescription ansDesc;
    RTCSessionDescriptionAsyncOperation answer;
    private void SetRemote2()
    {
        var oferta = offer_textbox.text;
        
        offerDesc = new RTCSessionDescription();
        offerDesc.sdp = oferta; //changed from oferta
        var op=_pc2.SetRemoteDescription(ref offerDesc);
        if (!op.IsError)
        {
            OnSetRemoteSuccess();
        }
        
    }
    private void CreateAns()
    {
        answer = _pc2.CreateAnswer();

    }
    public void SelLocal2()
    {
        ansDesc = new RTCSessionDescription();
        ansDesc = answer.Desc;
        var op = _pc2.SetLocalDescription(ref ansDesc);
        if (!op.IsError)
        {
            OnSetLocalSuccess();
        }
        answer_textbox.text = answer.Desc.sdp;
    }
    #endregion
    #region Createoffer
    RTCSessionDescriptionAsyncOperation offertest;
    private void creOff()
    {
        offertest = _pc1.CreateOffer();
        if (_pc1.SignalingState != RTCSignalingState.Stable)
        {
            Debug.Log("UnStable");
        }
    }
    private void SetLocal(RTCSessionDescription desc)
    { 
        var op = _pc1.SetLocalDescription(ref desc);

        if (!op.IsError)
        {
            OnSetLocalSuccess();
        }
        offer_textbox.text = offertest.Desc.sdp;
    }
    #endregion
    #region Data
    RTCDataChannel channel;
    RTCDataChannel channel2;
    public void CreateData()
    {

        var option = new RTCDataChannelInit();
        option.ordered=false;
        option.negotiated = false;
        channel = _pc1.CreateDataChannel("test", option);
        channel.OnMessage = bytes => 
        {
            Debug.Log(bytes);
        };
        channel.OnOpen = () => Debug.Log("Data 1 Open");
        channel.OnClose = () => Debug.Log("Data 1 Close");

        channel2 = _pc2.CreateDataChannel("test", option);
        channel2.OnMessage = bytes => 
        {
            Debug.Log(bytes);
        };
        channel2.OnOpen = () => Debug.Log("Data 2 Open");
        channel2.OnClose = () => Debug.Log("Data 2 CLose");
        
    }
    private void OnGUI()
    {
        
        /*if (GUILayout.Button("Create data channel"))
        {
            CreateData();
        }
        if (GUILayout.Button("Send msg"))
        {
            channel.Send("111");
            channel2.Send("111");
        }*/
        if(GUILayout.Button("CreateOffer 1"))
        {
            creOff();
        }
        if(GUILayout.Button("SetLocalDescription 1"))
        {
            SetLocal(offertest.Desc);
        }
        if(GUILayout.Button("SetRemoteDescription 2"))
        {
            SetRemote2();
        }
        if(GUILayout.Button("CreateAns"))
        {
            CreateAns();
        }
        if(GUILayout.Button("CheckAns"))
        {
            Debug.Log(answer.IsError);
            Debug.Log(answer.IsDone);
            Debug.Log(answer.keepWaiting);
        }
        if(GUILayout.Button("SelLocal 2"))
        {
            SelLocal2();
        }
        if(GUILayout.Button("SetRemote1"))
        {
            SetRemote1();
        }
        /*if(GUILayout.Button("AddMedia1"))
        {
            AddIce1();
        }
        if(GUILayout.Button("AddMedia2"))
        {
            AddIce2();
        }*/
        if(GUILayout.Button("Restart"))
        {
            AddIce3();
        }
    }
    #endregion Data
    private void OnSetLocalSuccess()
    {
        Debug.Log("SetLocalDescription complete");
    }
    private void OnSetRemoteSuccess()
    {
        Debug.Log("SetRemoteDescription complete");
    }

}
