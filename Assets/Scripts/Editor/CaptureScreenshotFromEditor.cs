using System;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unityエディタ上からGameビューのスクリーンショットを撮るEditor拡張
/// </summary>
public class CaptureScreenshotFromEditor : Editor
{
    /// <summary>
    /// キャプチャを撮る
    /// </summary>
    /// <remarks>
    /// Tool > Screenshot に追加。
    /// HotKeyは Ctrl + Shift + F1。
    /// </remarks>
    [MenuItem("Tool/Screenshot #%F1")]
    private static void CaptureScreenshot()
    {
        var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var fileName = DateTime.Now.ToString("スクリーンショット yyyy-MM-dd HH.mm.ss") + ".png";
        var exportPath = string.Format("{0}/{1}", folderPath, fileName);
        ScreenCapture.CaptureScreenshot(exportPath);
        File.Exists(exportPath);

        Debug.Log("ScreenShot: " + fileName);
    }
}
