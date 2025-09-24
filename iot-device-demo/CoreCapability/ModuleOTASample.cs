/*
 * Copyright (c) 2025-2025 Huawei Cloud Computing Technology Co., Ltd. All rights reserved.
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
using System.IO;
using System.Net;
using IoT.SDK.Device;
using IoT.SDK.Device.OTA;
using IoT.SDK.Device.Transport;
using IoT.SDK.Device.Utils;
using NLog;

namespace IoT.Device.Demo;

public class ModuleOTASample : DeviceSample
{
    /// <summary>
    /// Demonstrates how to upgrade devices.
    /// Usage: After creating an upgrade task on the platform, modify the device parameters in the main function and start this sample.
    /// The device receives the upgrade notification, downloads the upgrade package, and reports the upgrade result.
    /// The upgrade result is displayed on the platform.
    /// Prerequisites: \download\ The root directory must contain the download folder (which can be customized as required).
    /// </summary>
    protected override void RunDemo()
    {
  
    }
    
    protected override void BeforeInitDevice()
    {
        // The package path must contain the software or firmware package name and extension.
        string packageSavePath = IotUtil.GetRootDirectory() + @"\download";
        if (!Directory.Exists(packageSavePath))
        {
            Directory.CreateDirectory(packageSavePath);
        }

        var otaSample = new ModuleOTAUpgrade(Device, packageSavePath);
        otaSample.Init();
    }
    
    public class ModuleOTAUpgrade : ModuleOTAListener, ConnectListener
    {
        private static readonly Logger LOG = LogManager.GetCurrentClassLogger();

        private readonly OTAService otaService;
        private readonly IoTDevice device;

        private string version; // Version
        private string module; // Module
        private string eventId; // Module
        private readonly string packageSavePath; // Path where the upgrade Package is stored

        public ModuleOTAUpgrade(IoTDevice device, string packageSavePath)
        {
            this.device = device;
            device.GetClient().connectListener = this;
            otaService = device.otaService;
            otaService.SetModuleOtaListener(this);
            this.packageSavePath = packageSavePath;
            version = "v0.0.1"; // Change to the actual value.
            module = "mcu";
            eventId = "40cc9ab1-3579-488c-95c6-c18941c99eb4";
        }
        
        public void OnQueryVersion(ModuleOTAReportInfo reportInfo, string eventId)
        {
            if (reportInfo.code != 200)
            {
                LOG.Error("QueryVersion error= {}", reportInfo.ToString());
            }
        }

        public void OnNewPackage(ModuleOTAPackage otaPackage, string eventId)
        {
            LOG.Info("otaPackage = {}", otaPackage.ToString());
            otaPackage.module = module;
            version = new PackageHandler
            {
                ModulePackage = new ModulePackage
                {
                    Package = otaPackage,
                },
                OtaService = otaService,
                PackageSavePath = packageSavePath,
                EventId = eventId,
            }.Start() ?? version;
        }

        public void OnGetPackage(ModuleOTAReportInfo reportInfo, ModuleOTAPackage pkg, string eventId)
        {
            if (reportInfo.code != 200)
            {
                LOG.Error("QueryVersion error= {}", reportInfo.ToString());
            }
            else
            {
                OnNewPackage(pkg, eventId);
            }
        }

        public void onProgress(ModuleOTAReportInfo reportInfo, string eventId)
        {
            if (reportInfo.code != 200)
            {
                LOG.Error("QueryVersion error= {}", reportInfo.ToString());
            }
        }
        
        public void ConnectionLost()
        {
        }

        public void ConnectComplete()
        {
            otaService.ReportVersion(module, version, eventId);
        }

        public void ConnectFail()
        {
        }
        public int Init()
        {
            return 0;
        }
        
        private class OtaException : Exception
        {
            public int Result { get; set; }
            public int Progress { get; set; }
            public string Version { get; set; }
            
            public string Module { get; set; }
            public string Description { get; set; }
        }

        private class PackageHandler
        {
            private string packagePath;
            public ModulePackage ModulePackage { get; set; }
            public string PackageSavePath { get; set; }
            public OTAService OtaService { get; set; }
            
            public string EventId { get; set; }


            private void DownloadPackage()
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                // Declares an HTTP request.
                var myRequest = ModulePackage.GetWebRequest();

                // SSL security channel authentication certificate
                ServicePointManager.ServerCertificateValidationCallback = (a, b, c, d) => true;
                using var webResponse = myRequest.GetResponse();
                using var myStream = webResponse.GetResponseStream();
                packagePath = Path.Combine(PackageSavePath, ModulePackage.GetFileName());
                using var file = File.Open(packagePath, FileMode.Create);
                myStream.CopyTo(file);
                myStream.Flush();
            }

            private void VerifyPackageSign()
            {
                if (ModulePackage.GetSign() == null)
                {
                    LOG.Warn("sign is empty");
                    return;
                }

                if (ModulePackage.GetSignMethod() == "SHA256")
                {
                    var strSha256 = IotUtil.GetSHA256HashFromFile(packagePath);
                    LOG.Info("SHA256 = {}", strSha256);
                    if (strSha256 != ModulePackage.GetSign())
                    {
                        throw new OtaException
                        {
                            Result = OTAService.OTA_CODE_NO_NEED,
                            Progress = 0,
                            Version = ModulePackage.GetVersion(),
                            Module = ModulePackage.GetModule(),
                            Description = "sign verify failed"
                        };
                    }
                    LOG.Info("sign check passed");
                }
            }

            private void InstallPackage()
            {
                LOG.Info("install package ok");
                // throw new OtaException if the installation fails.
            }

            public string Start()
            {
                var module = ModulePackage.GetModule();
                try
                {
                    ModulePackage.PreCheck();
                    DownloadPackage();
                    VerifyPackageSign();
                    InstallPackage();
                    OtaService.ReportOtaStatus(OTAService.OTA_CODE_SUCCESS, 100, module, EventId, "upgrade success");
                    LOG.Info("ota upgrade ok");
                    return ModulePackage.GetVersion();
                }
                catch (OtaException ex)
                {
                    OtaService.ReportOtaStatus(ex.Result, ex.Progress, ex.Module, EventId, ex.Description);
                    LOG.Error("{}", ex.Description);
                }
                catch (WebException exp)
                {
                    OtaService.ReportOtaStatus(OTAService.OTA_CODE_DOWNLOAD_TIMEOUT, 0, module, EventId,
                        exp.GetBaseException().Message);
                    LOG.Error("download failed");
                }
                catch (Exception ex)
                {
                    OtaService.ReportOtaStatus(OTAService.OTA_CODE_INNER_ERROR, 0, module, EventId,
                        ex.GetBaseException().Message);
                    LOG.Error("download failed");
                }

                return null;
            }
        }

        private class ModulePackage
        {
            public ModuleOTAPackage Package { get; set; }

            public WebRequest GetWebRequest()
            {
                var myRequest = WebRequest.Create(new Uri(Package.url));
                return myRequest;
            }

            public string GetFileName()
            {
                return Package.fileName;
            }

            public string GetVersion()
            {
                return Package.version;
            }
            
            public string GetModule()
            {
                return Package.module;
            }

            public string GetSign()
            {
                return Package.sign;
            }
            
            public string GetSignMethod()
            {
                return Package.signMethod;
            }

            public void PreCheck()
            {
                // todo Check the version number, remaining space, remaining battery, and signal quality.
                // If the upgrade is not allowed, throw new OtaException  with error code defined in OTAService
                // or a custom error code.
            }
        }
    }
    
    
}