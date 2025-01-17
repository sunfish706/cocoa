﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Threading.Tasks;
using Covid19Radar.Services;
using Covid19Radar.Model;

namespace Covid19Radar.iOS.Services
{
    public class DeviceCheckService : IDeviceVerifier
    {
		public async Task<string> VerifyAsync(DiagnosisSubmissionParameter _)
		{
			var token = await DeviceCheck.DCDevice.CurrentDevice.GenerateTokenAsync();
			return Convert.ToBase64String(token.ToArray());
		}

        public async Task<string> VerifyAsync(V1EventLogRequest _)
        {
			var token = await DeviceCheck.DCDevice.CurrentDevice.GenerateTokenAsync();
			return Convert.ToBase64String(token.ToArray());
		}
	}
}