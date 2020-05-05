// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

namespace Azure.Iot.Hub.Service.Models
{
    /// <summary> The ServiceStatistics. </summary>
    public partial class ServiceStatistics
    {
        /// <summary> Initializes a new instance of ServiceStatistics. </summary>
        internal ServiceStatistics()
        {
        }

        /// <summary> Initializes a new instance of ServiceStatistics. </summary>
        /// <param name="connectedDeviceCount"> . </param>
        internal ServiceStatistics(long? connectedDeviceCount)
        {
            ConnectedDeviceCount = connectedDeviceCount;
        }

        public long? ConnectedDeviceCount { get; }
    }
}