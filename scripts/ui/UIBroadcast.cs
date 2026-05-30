using Godot;
using System;
using System.Collections.Generic;

[GlobalClass]
public partial class UIBroadcast : RichTextLabel
{
    [Export]
    public string MessageFormat = "[center]{0}[/center]";

    public struct BCMessage
    {
        public string Message;
        public double TimeVisible;

        public BCMessage(string msg, double t)
        {
            Message = msg;
            TimeVisible = t;
        }
    }

    public Queue<BCMessage> queue = new Queue<BCMessage>();
    private double timer;

    public override void _Process(double delta)
    {
        timer -= delta;
        if (timer <= 0d)
        {
            if (queue.Count > 0)
            {
                BCMessage msg = queue.Dequeue();
                timer = msg.TimeVisible;
                Text = string.Format(MessageFormat, msg.Message);
            }
            else
            {
                Text = string.Empty;
                timer = 0d;
            }
        }
    }

    public void AddMessage(string msg, double t)
    {
        queue.Enqueue(new BCMessage(msg, t));
    }
}
