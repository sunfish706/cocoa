﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System.Threading.Tasks;
using Covid19Radar.Model;

namespace Covid19Radar.Services
{
    public class DeviceVerifierMock : IDeviceVerifier
    {
        public Task<string> VerifyAsync(DiagnosisSubmissionParameter _)
            => Task.Run(() =>　"DUMMY RESPONSE");

        public Task<string> VerifyAsync(V1EventLogRequest _)
            => Task.Run(() => "DUMMY RESPONSE");
    }
}
