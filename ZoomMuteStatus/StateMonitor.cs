using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows.Automation;

namespace ZoomMuteStatus
{
    public class StateMonitor
    {
        private static List<string> MonitoringWindowClass = new List<string>
        {
            "ZPFloatToolbarClass",
            "ZPContentViewWndClass",
            "ZPFloatVideoWndClass",
            "ZPActiveSpeakerWndClass"
        };

        private bool Running;
        private AudioState PreviousState;
        
        public void Start()
        {
            Running = true;
            var audioState = AudioState.Unknown;
            var sw = new Stopwatch();
            while (Running)
            {
                sw.Restart();
                var zoomProcesses = Process.GetProcessesByName("Zoom");
                if (zoomProcesses.Length == 0)
                {
                    Console.WriteLine("can not find zoom process, wait 10s");
                    continue;
                }
                
                if (zoomProcesses.Length <= 1)
                {
                    Console.WriteLine("wait zoom starts ready");
                    continue;
                }
                
                var zoomWindows = GetZoomWindows(zoomProcesses.Select(p => p.Id).ToList());
                if (zoomWindows.Count == 0)
                {
                    audioState = AudioState.Unknown;
                }
                foreach (AutomationElement zoomWindow in zoomWindows)
                {
                    try
                    {
                        var windowElementTree = new TreeNode<AutomationElement>(zoomWindow);
                        WalkElement(zoomWindow, windowElementTree);
                        audioState = GetAudioStateByWindow(windowElementTree);
                        if (audioState != AudioState.Unknown) break;
                    }
                    catch (ElementNotAvailableException)
                    {
                        Console.WriteLine("can not get audio state from window, since window could be closed");
                    }
                }
                
                sw.Stop();
                Console.Clear();
                Console.Write($"Audio state: {audioState}({sw.ElapsedMilliseconds} ms)\r");
                TurnOnLight(audioState);
                Thread.Sleep(1000);
            }
        }

        public void Stop()
        {
            Running = false;
        }

        private void TurnOnLight(AudioState state)
        {
            if (PreviousState == state) return;
            var exePath = "C:\\blink-tool\\blink1-tool.exe";
            var args = string.Empty;
            if (state == AudioState.Unknown)
            {
                args = "--off";
            }
            else
            {
                args = state == AudioState.Unmuted ? "--red" : "--green";
            }

            var cmd = new Process();
            cmd.StartInfo.FileName = exePath;
            cmd.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmd.StartInfo.Arguments = args;
            cmd.Start();
            PreviousState = state;
        }

        private AutomationElementCollection GetZoomWindows(List<int> processIds)
        {
            var classNameCondition = new OrCondition(MonitoringWindowClass
                .Select(cls => new PropertyCondition(AutomationElementIdentifiers.ClassNameProperty, cls)).ToArray());
            var processIdsCondition = new OrCondition(processIds
                .Select(pid => new PropertyCondition(AutomationElementIdentifiers.ProcessIdProperty, pid)).ToArray());
            var controlTypeCondition =
                new PropertyCondition(AutomationElementIdentifiers.ControlTypeProperty, ControlType.Window);
            return AutomationElement.RootElement
                .FindAll(TreeScope.Children,
                    new AndCondition(processIdsCondition, controlTypeCondition, classNameCondition));
        }

        private void WalkElement(AutomationElement element, TreeNode<AutomationElement> treeNode)
        {
            var node = TreeWalker.ControlViewWalker.GetFirstChild(element);
            while (node != null)
            {
                var childTreeNode = new TreeNode<AutomationElement>(node);
                treeNode.Nodes.Add(childTreeNode);
                WalkElement(node, childTreeNode);
                node = TreeWalker.ControlViewWalker.GetNextSibling(node);
            }
        }

        private void PrintTree(TreeNode<AutomationElement> tree, int level = 0)
        {
            var indent = new string(' ', level * 4);
            Console.WriteLine($"{indent}{tree.Current.ToHumanReadable()}");
            var nodeIndent = level + 1;
            foreach (var node in tree.Nodes)
            {
                PrintTree(node, nodeIndent);
            }
        }

        private AudioState GetAudioStateByWindow(TreeNode<AutomationElement> windowTreeNode)
        {
            var buttonElements = windowTreeNode
                .GetElements()
                .Where(e => e.Current.ControlType.ProgrammaticName == ControlType.Button.ProgrammaticName)
                .ToList();
            if (buttonElements.Any(be =>
                    be.Current.Name.Contains("currently unmuted") || be.Current.Name.Contains("Mute My Audio")))
                return AudioState.Unmuted;
            if (buttonElements.Any(be =>
                    be.Current.Name.Contains("currently muted") || be.Current.Name.Contains("Unmute My Audio")))
                return AudioState.Muted;
            return AudioState.Unknown;
        }
    }
}