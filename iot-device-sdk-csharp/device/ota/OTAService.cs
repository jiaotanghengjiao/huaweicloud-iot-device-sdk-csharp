/*
 * Copyright (c) 2020-2024 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice, this list of
 *    conditions and the following disclaimer.
 *
 * 2. Redistributions in binary form must reproduce the above copyright notice, this list
 *    of conditions and the following disclaimer in the documentation and/or other materials
 *    provided with the distribution.
 *
 * 3. Neither the name of the copyright holder nor the names of its contributors may be used
 *    to endorse or promote products derived from this software without specific prior written
 *    permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO,
 * THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR
 * PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS;
 * OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY,
 * WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR
 * OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using IoT.SDK.Device.Client.Requests;
using IoT.SDK.Device.Service;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.SDK.Device.OTA
{
    /// <summary>
    /// Provides APIs related to OTA upgrades.
    /// </summary>
    public class OTAService : AbstractService
    {
        // Error codes reported during an upgrade. You can also define your own error codes.
        public static readonly int OTA_CODE_SUCCESS = 0; // Upgraded.
        public static readonly int OTA_CODE_BUSY = 1; // The device is in use.
        public static readonly int OTA_CODE_SIGNAL_BAD = 2; // Poor signal.
        public static readonly int OTA_CODE_NO_NEED = 3; // Already the latest version.
        public static readonly int OTA_CODE_LOW_POWER = 4; // Low battery.
        public static readonly int OTA_CODE_LOW_SPACE = 5; // Insufficient free space.
        public static readonly int OTA_CODE_DOWNLOAD_TIMEOUT = 6; // Download timed out.
        public static readonly int OTA_CODE_CHECK_FAIL = 7; // Upgrade package verification failed.
        public static readonly int OTA_CODE_UNKNOWN_TYPE = 8; // Unsupported upgrade package type.
        public static readonly int OTA_CODE_LOW_MEMORY = 9; // Insufficient memory.
        public static readonly int OTA_CODE_INSTALL_FAIL = 10; // Upgrade package installation failed.
        public static readonly int OTA_CODE_INNER_ERROR = 255; // Internal exception.

        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private OTAListener otaListener;

        private ModuleOTAListener moduleOtaListener;

        public override string GetServiceId()
        {
            return "$ota";
        }

        /// <summary>
        /// Sets an OTA listener.
        /// </summary>
        /// <param name="otaListener">Indicates the OTA listener to set.</param>
        public void SetOtaListener(OTAListener otaListener)
        {
            this.otaListener = otaListener;
        }

        /// <summary>
        /// Sets an Module OTA listener.
        /// </summary>
        /// <param name="otaListener">Indicates the OTA listener to set.</param>
        public void SetModuleOtaListener(ModuleOTAListener moduleOtaListener)
        {
            this.moduleOtaListener = moduleOtaListener;
        }

        /// <summary>
        /// Called when an OTA upgrade event is processed.
        /// </summary>
        /// <param name="deviceEvent">Indicates an event.</param>
        public override void OnEvent(DeviceEvent deviceEvent)
        {
            
            if (otaListener != null && deviceEvent.eventType == "version_query")
            {
                OTAQueryInfo queryInfo = JsonUtil.ConvertDicToObject<OTAQueryInfo>(deviceEvent.paras);
                otaListener.OnQueryVersion(queryInfo);
            }
            else if (otaListener != null &&
                     (deviceEvent.eventType == "firmware_upgrade" || deviceEvent.eventType == "software_upgrade"))
            {
                OTAPackage pkg = JsonUtil.ConvertDicToObject<OTAPackage>(deviceEvent.paras);

                // A separate thread is started for the OTA upgrade.
                new Thread(new ThreadStart(new Action(() =>
                {
                    // to do Write code used to start the new thread.
                    otaListener.OnNewPackage(pkg);
                }))).Start();
            }
            else if (otaListener != null && 
                     deviceEvent.eventType == "firmware_upgrade_v2" || deviceEvent.eventType == "software_upgrade_v2")
            {
                OTAPackageV2 pkgV2 = JsonUtil.ConvertDicToObject<OTAPackageV2>(deviceEvent.paras);
                // A separate thread is started for the OTA upgrade.
                new Thread(new ThreadStart(new Action(() =>
                {
                    // to do Write code used to start the new thread.
                    otaListener.OnNewPackageV2(pkgV2);
                }))).Start();
            }
            else if (moduleOtaListener != null && deviceEvent.eventType == "module_version_report_response")
            {
                ModuleOTAReportInfo queryInfo = JsonUtil.ConvertDicToObject<ModuleOTAReportInfo>(deviceEvent.paras);
                moduleOtaListener.OnQueryVersion(queryInfo, deviceEvent.eventId);
            }
            else if (moduleOtaListener != null && deviceEvent.eventType == "module_upgrade_notify")
            {
                ModuleOTAPackage pkg = JsonUtil.ConvertDicToObject<ModuleOTAPackage>(deviceEvent.paras);
                // A separate thread is started for the OTA upgrade.
                new Thread(new ThreadStart(new Action(() =>
                {
                    // to do Write code used to start the new thread.
                    moduleOtaListener.OnNewPackage(pkg, deviceEvent.eventId);
                }))).Start();
            }
            else if (moduleOtaListener != null && deviceEvent.eventType == "module_progress_report_response")
            {
                ModuleOTAReportInfo queryInfo = JsonUtil.ConvertDicToObject<ModuleOTAReportInfo>(deviceEvent.paras);
                moduleOtaListener.onProgress(queryInfo, deviceEvent.eventId);
            }
            else if (moduleOtaListener != null && deviceEvent.eventType == "module_package_get_response")
            {
                ModuleOTAReportInfo queryInfo = JsonUtil.ConvertDicToObject<ModuleOTAReportInfo>(deviceEvent.paras);
                ModuleOTAPackage pkg = JsonUtil.ConvertDicToObject<ModuleOTAPackage>(deviceEvent.paras);
                new Thread(new ThreadStart(new Action(() =>
                {
                    // to do Write code used to start the new thread.
                    moduleOtaListener.OnGetPackage(queryInfo, pkg, deviceEvent.eventId);
                }))).Start();
            }
        }


        /// <summary>
        /// Reports a firmware version.
        /// </summary>
        /// <param name="version">Indicates the firmware version.</param>
        public void ReportVersion(string version)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();

            node.Add("fw_version", version);
            node.Add("sw_version", version);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "version_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        /// <summary>
        /// Reports the upgrade status.
        /// </summary>
        /// <param name="result">Indicates the upgrade result.</param>
        /// <param name="progress">Indicates the upgrade progress, ranging from 0 to 100.</param>
        /// <param name="version">Indicates the current version.</param>
        /// <param name="description">Indicates the description of the failure. It is optional.</param>
        public void ReportOtaStatus(int result, int progress, string version, string description)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();
            node.Add("result_code", result);
            node.Add("progress", progress);
            if (description != null)
            {
                node.Add("description", description);
            }

            node.Add("version", version);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "upgrade_progress_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }


        /// <summary>
        /// Reports a firmware version.
        /// </summary>
        /// <param name="module">Module to which the device belongs.</param>
        /// <param name="version">Indicates the firmware version.</param>
        /// <param name="eventId">The unique identifier associated with this package retrieval event.
        /// Used for tracking and correlating events on the IoT platform.</param>
        public void ReportVersion(string module, string version, string eventId)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();

            node.Add("module", module);
            node.Add("version", version);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "module_version_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();
            deviceEvent.eventId = eventId;

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        /// <summary>
        /// Reports the upgrade status.
        /// </summary>
        /// <param name="result">Indicates the upgrade result.</param>
        /// <param name="progress">Indicates the upgrade progress, ranging from 0 to 100.</param>
        /// <param name="module">Module to which the device belongs.</param>
        /// <param name="eventId">The unique identifier associated with this package retrieval event.
        /// Used for tracking and correlating events on the IoT platform.</param>
        /// <param name="description">Indicates the description of the failure. It is optional.</param>
        public void ReportOtaStatus(int result, int progress, string module, string eventId, string description)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();
            node.Add("result_code", -result);
            node.Add("progress", progress);
            if (description != null)
            {
                node.Add("description", description);
            }

            node.Add("module", module);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "module_progress_report";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();
            deviceEvent.eventId = eventId;

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }

        /// <summary>
        /// Proactively obtain upgrade packages.
        /// </summary>
        /// <param name="module">Module to which the device belongs.</param>
        /// <param name="eventId">The unique identifier associated with this package retrieval event.
        /// Used for tracking and correlating events on the IoT platform.</param>
        public void ReportPackageGet(string module, string eventId)
        {
            Dictionary<string, object> node = new Dictionary<string, object>();
            node.Add("module", module);

            DeviceEvent deviceEvent = new DeviceEvent();
            deviceEvent.eventType = "module_package_get";
            deviceEvent.paras = node;
            deviceEvent.serviceId = "$ota";
            deviceEvent.eventTime = IotUtil.GetEventTime();
            deviceEvent.eventId = eventId;

            iotDevice.GetClient().ReportEvent(deviceEvent);
        }
    }
}