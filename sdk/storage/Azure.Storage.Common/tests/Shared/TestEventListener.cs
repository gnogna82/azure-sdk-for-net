﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using Azure.Core.Diagnostics;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Azure.Storage.Test
{
    /// <summary>
    /// The TestEventListener listens for the AzureSDK logging event source
    /// and traces the output so it's easy to view the logs when testing.
    ///
    /// Simply create an instance of the TestEventListener before you start
    /// running your tests.
    /// </summary>
    internal class TestEventListener : IDisposable
    {
        private StringBuilder _eventBuffer;

        private readonly AzureEventSourceListener _eventSourceListener;

        public TestEventListener()
        {
            _eventSourceListener = new AzureEventSourceListener(
                (e, _) => LogEvent(e),
                EventLevel.Verbose);
        }

        /// <summary>
        /// Sets up the Event listener buffer for the test about to run.
        /// </summary>
        public void SetupEventsForTest()
        {
            _eventBuffer = new StringBuilder();
        }

        /// <summary>
        /// Output the Events to the console in the case of test failure.
        /// This will include the HTTP requests and responses.
        /// </summary>
        public void OutputEventsForTest()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                TestContext.Out.WriteLine(_eventBuffer.ToString());
            }
        }

        /// <summary>
        /// Trace any SDK events.
        /// </summary>
        /// <param name="args">Event arguments.</param>
        public void LogEvent(EventWrittenEventArgs args)
        {
            var category = args.EventName;
            IDictionary<string, string> payload = GetPayload(args);

            // If there's a request ID, use it after the category
            var message = new StringBuilder();
            if (payload.TryGetValue("requestId", out var requestId))
            {
                payload.Remove("requestId");
                message.Append(requestId);
            }
            message.AppendLine();

            // Add the rest of the payload
            foreach (KeyValuePair<string, string> arg in payload)
            {
                message.AppendFormat("  {0}: ", arg.Key);

                // Don't indent the content's lines
                if (arg.Key == "content" && arg.Value.Length > 0)
                {
                    message.AppendLine("\n" + arg.Value);
                    continue;
                }

                var lines = arg.Value.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 1)
                {
                    // If there's only one line, write it after the name
                    message.AppendLine(lines[0]);
                }
                else
                {
                    // Otherwise add a newline and indent each nested line
                    message.AppendLine();
                    foreach (var line in lines.Select(l => $"    {l}"))
                    {
                        message.AppendLine(line);
                    }
                }
            }

            // Dump the message and category
            Trace.WriteLine(message, category);

            // Add the message to event buffer
            Assert.IsNotNull(
                _eventBuffer,
                "SetupEventsForTest needs to be called before each test when using TestEventListener.");
            _eventBuffer.Append(message);
            _eventBuffer.AppendLine();
        }

        /// <summary>
        /// Convert the event arguments into a dictionary of strings.
        /// </summary>
        /// <param name="args">The event arguments.</param>
        /// <returns>A dictionary of strings.</returns>
        private static IDictionary<string, string> GetPayload(EventWrittenEventArgs args)
        {
            var payload = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < args.Payload.Count; i++)
            {
                var name = args.PayloadNames[i];
                var value = "";
                switch (args.Payload[i])
                {
                    case null:
                        break;
                    case string s:
                        value = s;
                        break;
                    case byte[] content:
                        value = Encoding.UTF8.GetString(content);
                        // Control characters mess up copy/pasting so we'll
                        // swap them with the SUB character
                        value = new string(value.Select(ch => !char.IsControl(ch) ? ch : '�').ToArray());
                        break;
                    default:
                        value = args.Payload[i].ToString();
                        break;
                }
                payload.Add(name, value);
            }
            return payload;
        }

        /// <summary>
        /// Cleans up the <see cref="AzureEventSourceListener"/> instance.
        /// </summary>
        public void Dispose() => _eventSourceListener.Dispose();
    }
}