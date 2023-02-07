using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine.UI;
using UnityEngine;
using TMPro;
using System.Linq;
using System;

public class p2p2 : MonoBehaviour
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

    [SerializeField] private Button createOffer_btn;
    [SerializeField] private Button createAnswer_btn;
    [SerializeField] private Button addAnswer_btn;
    [SerializeField] private TMP_InputField offer_textbox;
    [SerializeField] private TMP_InputField answer_textbox;


    private RTCPeerConnection _pc1;
    private MediaStream localStream;
    private MediaStream remoteStream;
    

    private RTCSessionDescriptionAsyncOperation offer;

    private AudioClip m_clipInput;
    private AudioStreamTrack m_audioTrack;

    int m_samplingFrequency = 48000;
    int m_lengthSeconds = 1;

    private void Awake()
    {
        createOffer_btn.onClick.AddListener(creOff);
        createAnswer_btn.onClick.AddListener(creAns);
        addAnswer_btn.onClick.AddListener(addAnswer);
    }

   




    // Start is called before the first frame update
    void Start()
    {
        microphone = Microphone.devices[0];
        Debug.Log(microphone);

        StartCoroutine(WebRTC.Update());
        m_clipInput = Microphone.Start(microphone, true, m_lengthSeconds, m_samplingFrequency);

        inputAudioSource.loop = true;
        inputAudioSource.clip = m_clipInput;
        //inputAudioSource.Play();

        m_audioTrack = new AudioStreamTrack(inputAudioSource);
        m_audioTrack.Loopback = true;

        remoteStream = new MediaStream();

        remoteStream.OnAddTrack += OnAddTrack;

        var configuration = GetSelectedSdpSemantics();
        _pc1 = new RTCPeerConnection(ref configuration);
        //_pc1.OnIceCandidate += (e) => handleIceAns(e);
        _pc1.AddTrack(m_audioTrack, localStream);

        /*
        _receiveStream = new MediaStream();
        _receiveStream.OnAddTrack += OnAddTrack;
        _sendStream = new MediaStream();

       
       
        var sender = _pc1.AddTrack(track, _sendStream);
        */

    }

    void OnAddTrack(MediaStreamTrackEvent e)
    {
        var track = e.Track as AudioStreamTrack;
        outputAudioSource.SetTrack(track);
        outputAudioSource.loop = true;
        outputAudioSource.Play();

    }

    private static RTCConfiguration GetSelectedSdpSemantics()
    {
        RTCConfiguration config = default;
        config.iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } };
        return config;
    }

    // Update is called once per frame
    private void Update()
    {
        while (threadPumpList.Count > 0)
        {
           if(threadPumpList[0] == null)
            {
                threadPumpList.RemoveAt(0);
            }
            StartCoroutine(threadPumpList[0]);
            
            threadPumpList.RemoveAt(0);
        }
    }


    private void addAnswer()
    {
        Debug.Log("AddAnswer()");
        RTCSessionDescription desc = new RTCSessionDescription();
        var answer = answer_textbox.text;
        desc.sdp = answer;
        _pc1.SetLocalDescription(ref desc);
    }

    private void creAns()
    {
        Debug.Log("creAns()");
       

        threadPumpList.Add(addRemoteDesc());

    }



    private IEnumerator addRemoteDesc()
    {
        var oferta = offer_textbox.text;
        Debug.Log(oferta);
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.sdp = oferta;
        yield return _pc1.SetRemoteDescription(ref desc);
        Debug.Log(_pc1.RemoteDescription.sdp);

        var answer= _pc1.CreateAnswer();
        yield return answer;
        answer_textbox.text = answer.Desc.sdp;

    }

    /*
        RTCSessionDescription desc = new RTCSessionDescription();
        var offer = offer_textbox.text;
        desc.sdp = offer;
        _pc1.SetRemoteDescription(ref desc);
        Debug.Log(offer);
        RTCSessionDescription desc2 = new RTCSessionDescription();
        var answer = _pc1.CreateAnswer();
        desc2.sdp = answer.Desc.sdp;
        Debug.Log(answer.Desc.sdp);
        yield return _pc1.SetLocalDescription(ref desc2);
        //
        //yield return 


        Debug.Log("creAns()");
        _pc1.OnIceCandidate += (e) => handleIceAns(e);
        RTCSessionDescription desc = new RTCSessionDescription();
        desc.sdp = offer_textbox.text;
        Debug.Log(desc.sdp);
        _pc1.SetRemoteDescription(ref desc);
        RTCSessionDescription desc2 = new RTCSessionDescription();
        var answer = _pc1.CreateAnswer();
        Debug.Log(answer.);
        desc2.sdp = answer.Desc.sdp;
        Debug.Log(desc2.sdp);
        _pc1.SetLocalDescription(ref desc2);

        //threadPumpList.Add(setRD());
        //threadPumpList.Add(cA());
    
    */


    private void creOff()
    {
        Debug.Log("creOff()");
        _pc1.OnIceCandidate += (e) => handleIce(e);

        threadPumpList.Add(subCreateOffer());
        
    }

    private IEnumerator subCreateOffer()
    {
        var offer = _pc1.CreateOffer();
        yield return offer;
        if (offer != null)
        {
           // Debug.Log(offer.Desc);
            threadPumpList.Add(OnCreateOffer(_pc1, offer.Desc));
        }
        
       
    }

    

    private IEnumerator OnCreateOffer(RTCPeerConnection _pc1, RTCSessionDescription desc)
    {
        
        var op = _pc1.SetLocalDescription(ref desc);
        if (op != null)
        {
            Debug.Log("success!");
            offer_textbox.text = _pc1.LocalDescription.sdp;
        }
        
        return null;
    }

    private void handleIce(RTCIceCandidate candidate)
    {
        if (candidate != null)
        {
            Debug.Log("KANDYDANT!");
            Debug.Log(candidate.Candidate);
            offer_textbox.text = _pc1.LocalDescription.sdp;
        }
        
    }
    private void handleIceAns(RTCIceCandidate candidate)
    {
        if (candidate != null)
        {
            Debug.Log("KANDYDANT!");
            Debug.Log(candidate.Candidate);
            answer_textbox.text = _pc1.LocalDescription.sdp;
        }

    }
}
