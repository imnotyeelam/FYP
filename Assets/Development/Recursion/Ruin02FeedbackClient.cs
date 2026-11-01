using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace Fyp.Development.Recursion
{
    public enum Ruin02FeedbackSource { Pending, LiveAI, LocalFallback, Offline, HttpError, Timeout, InvalidJson }

    [Serializable]
    public sealed class Ruin02FeedbackRequest
    {
        public string playerAnswer;
        public string ruinId="Ruin02";
        public int currentStep;
        public int currentN;
        public string phase;
        public string expectedOperation;
        public int attemptCount;
        public string functionCode;
    }

    public readonly struct Ruin02FeedbackResult
    {
        public Ruin02FeedbackSource Source { get; }
        public string Text { get; }
        public long HttpStatus { get; }
        public Ruin02FeedbackResult(Ruin02FeedbackSource source,string text,long httpStatus=0)
        {Source=source;Text=text;HttpStatus=httpStatus;}
    }

    // Same POST /feedback endpoint; structured optional fields keep Portal requests compatible.
    public sealed class Ruin02FeedbackClient : MonoBehaviour
    {
        public AIFeedbackManager existingEndpoint;
        [Min(1)] public int timeoutSeconds=10;
        readonly HashSet<string> logged=new HashSet<string>();
        [Serializable] sealed class Response { public string feedback; public string source; }
        void Diagnose(Ruin02FeedbackResult result)
        {
            string code=result.Source==Ruin02FeedbackSource.HttpError?
                result.Source+" "+result.HttpStatus:result.Source.ToString();
            if(!logged.Add(code))return;
            if(result.Source==Ruin02FeedbackSource.LiveAI)
                Debug.Log("Ruin02 feedback: live AI response received.",this);
            else
                Debug.LogWarning("Ruin02 feedback: "+code+"; local fallback used.",this);
        }
        public IEnumerator Generate(Ruin02FeedbackRequest data,Action<Ruin02FeedbackResult> complete)
        {
            if(existingEndpoint==null || string.IsNullOrWhiteSpace(existingEndpoint.backendURL))
            {
                var offline=new Ruin02FeedbackResult(Ruin02FeedbackSource.Offline,null);
                Diagnose(offline);complete?.Invoke(offline);yield break;
            }
            var bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(data));
            using(var request=new UnityWebRequest(existingEndpoint.backendURL,"POST"))
            {
                request.uploadHandler=new UploadHandlerRaw(bytes);
                request.downloadHandler=new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type","application/json");request.timeout=timeoutSeconds;
                yield return request.SendWebRequest();
                Ruin02FeedbackResult result;
                if(request.result!=UnityWebRequest.Result.Success)
                {
                    var source=request.responseCode>0?Ruin02FeedbackSource.HttpError:
                        request.error!=null && request.error.IndexOf("timeout",StringComparison.OrdinalIgnoreCase)>=0?
                        Ruin02FeedbackSource.Timeout:Ruin02FeedbackSource.Offline;
                    result=new Ruin02FeedbackResult(source,null,request.responseCode);
                }
                else
                {
                    Response parsed=null;
                    try{parsed=JsonUtility.FromJson<Response>(request.downloadHandler.text);}catch(ArgumentException){}
                    result=parsed!=null && !string.IsNullOrWhiteSpace(parsed.feedback) && parsed.source=="openai"?
                        new Ruin02FeedbackResult(Ruin02FeedbackSource.LiveAI,TrimFeedback(parsed.feedback),request.responseCode):
                        new Ruin02FeedbackResult(Ruin02FeedbackSource.InvalidJson,null,request.responseCode);
                }
                Diagnose(result);complete?.Invoke(result);
            }
        }
        static string TrimFeedback(string feedback)
        {
            string text=feedback.Trim();int sentenceEnds=0;
            for(int i=0;i<text.Length;i++)
                if((text[i]=='.'||text[i]=='!'||text[i]=='?') && ++sentenceEnds==3)
                {text=text.Substring(0,i+1);break;}
            if(text.Length>350)text=text.Substring(0,350).TrimEnd()+"…";
            return text;
        }
    }
}
