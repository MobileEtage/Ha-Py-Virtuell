using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonShowInfo : MonoBehaviour
{
    public string title = "";
    [TextArea(3, 10)]
    public string description = "";

    public void ExecuteEvent()
    {
        InfoController.instance.ShowMessage(title, description);
    }
}
