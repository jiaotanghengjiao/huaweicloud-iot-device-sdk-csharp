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

namespace IoT.SDK.Device.OTA
{
    /// <summary>
    /// Provides listeners to listen to OTA upgrades.
    /// </summary>
    public interface ModuleOTAListener
    {
        /// <summary>
        /// Called when a version query request is received from the OTA server.
        /// </summary>
        /// <param name="reportInfo">Contains device version reporting information</param>
        void OnQueryVersion(ModuleOTAReportInfo info, string eventId);

        /// <summary>
        /// Called when a new upgrade package is available or received.
        /// </summary>
        /// <param name="pkg">Represents the new version package metadata</param>
        void OnNewPackage(ModuleOTAPackage pkg, string eventId);

        /// <summary>
        /// Called when an upgrade package is successfully retrieved or downloaded.
        /// </summary>
        /// <param name="reportInfo">Contains OTA reporting and status information</param>
        /// <param name="pkg">Represents the retrieved upgrade package object</param>
        void OnGetPackage(ModuleOTAReportInfo info, ModuleOTAPackage pkg, string eventId);
        
        /// <summary>
        /// Called when OTA upgrade progress is updated during package download or installation.
        /// </summary>
        /// <param name="pkg">Contains progress reporting information for the OTA operation</param>
        void onProgress(ModuleOTAReportInfo info, string eventId);
    }
}
