using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class ObserverManager<T> where T : Enum
{
    private static Dictionary<T, Action<object>> _boardObserver = new Dictionary<T, Action<object>>();

    public static void AddDesgisterEvent(T evenID, Action<object> callback)
    {
        if (callback == null) return;
        if (evenID == null) return;

        if(!_boardObserver.TryAdd(evenID, callback))
        {
            _boardObserver[evenID] += callback;
            //return;
        }
        
    }

    public static void PostEven(T evenID, object parant = null)
    {
        if (!_boardObserver.ContainsKey(evenID))
        {
            Debug.Log("Action is null , chua co even dky su kien");
            return;
        }

        if (_boardObserver[evenID] == null)
        {
            _boardObserver.Remove(evenID);
        }

        _boardObserver[evenID]?.Invoke(parant);
    }

    public static void RemoveAddListener(T evenID, Action<object> callback)
    {
        if (!_boardObserver.ContainsKey(evenID))
        {
            return;
        }
        _boardObserver[evenID] -= callback;

        if (_boardObserver[evenID] == null)
        {
            _boardObserver.Remove(evenID);
            return;
        }
    }

    public static void RemoveAll()
    {
        _boardObserver.Clear();
    }
}
