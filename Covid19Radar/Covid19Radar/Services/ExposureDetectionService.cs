﻿/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chino;
using Covid19Radar.Repository;
using Covid19Radar.Services.Logs;
using Xamarin.Essentials;

namespace Covid19Radar.Services
{
    public interface IExposureDetectionService
    {
        public void DiagnosisKeysDataMappingApplied();

        public void PreExposureDetected(ExposureConfiguration exposureConfiguration, string enVersion);

        public void ExposureDetected(ExposureConfiguration exposureConfiguration, string enVersion, IList<DailySummary> dailySummaries, IList<ExposureWindow> exposureWindows);

        public void ExposureDetected(ExposureConfiguration exposureConfiguration, string enVersion, ExposureSummary exposureSummary, IList<ExposureInformation> exposureInformations);

        public void ExposureNotDetected(ExposureConfiguration exposureConfiguration, string enVersion);
    }

    public class ExposureDetectionService : IExposureDetectionService
    {
        private readonly ILoggerService _loggerService;
        private readonly IUserDataRepository _userDataRepository;
        private readonly ILocalNotificationService _localNotificationService;
        private readonly IExposureRiskCalculationService _exposureRiskCalculationService;

        private readonly IExposureConfigurationRepository _exposureConfigurationRepository;

        private readonly IExposureDataCollectServer _exposureDataCollectServer;

        public ExposureDetectionService
        (
            ILoggerService loggerService,
            IUserDataRepository userDataRepository,
            ILocalNotificationService localNotificationService,
            IExposureRiskCalculationService exposureRiskCalculationService,
            IExposureConfigurationRepository exposureConfigurationRepository,
            IExposureDataCollectServer exposureDataCollectServer
            )
        {
            _loggerService = loggerService;
            _userDataRepository = userDataRepository;
            _localNotificationService = localNotificationService;
            _exposureRiskCalculationService = exposureRiskCalculationService;
            _exposureConfigurationRepository = exposureConfigurationRepository;
            _exposureDataCollectServer = exposureDataCollectServer;
        }

        public void DiagnosisKeysDataMappingApplied()
        {
            _loggerService.StartMethod();

            if (_exposureConfigurationRepository.IsDiagnosisKeysDataMappingConfigurationUpdated())
            {
                _exposureConfigurationRepository.SetDiagnosisKeysDataMappingAppliedDateTime(DateTime.UtcNow);
                _exposureConfigurationRepository.SetDiagnosisKeysDataMappingConfigurationUpdated(false);
            }

            _loggerService.EndMethod();
        }

        public void PreExposureDetected(ExposureConfiguration exposureConfiguration, string enVersion)
        {
            _loggerService.Debug("PreExposureDetected");
        }

        public void ExposureDetected(ExposureConfiguration exposureConfiguration, string enVersion, IList<DailySummary> dailySummaries, IList<ExposureWindow> exposureWindows)
        {
            _loggerService.Debug("ExposureDetected: ExposureWindows");

            _ = Task.Run(async () =>
            {
                await _userDataRepository.SetExposureDataAsync(
                    dailySummaries.ToList(),
                    exposureWindows.ToList()
                    );

                bool isHighRiskExposureDetected = dailySummaries
                    .Select(dailySummary => _exposureRiskCalculationService.CalcRiskLevel(dailySummary))
                    .Where(riskLevel => riskLevel >= RiskLevel.High)
                    .Count() > 0;

                if (isHighRiskExposureDetected)
                {
                    _ = _localNotificationService.ShowExposureNotificationAsync();
                }
                else
                {
                    _loggerService.Info($"DailySummary: {dailySummaries.Count}, but no high-risk exposure detected");
                }

                await _exposureDataCollectServer.UploadExposureDataAsync(
                    exposureConfiguration,
                    DeviceInfo.Model,
                    enVersion,
                    dailySummaries, exposureWindows
                    );
            });
        }

        public void ExposureDetected(ExposureConfiguration exposureConfiguration, string enVersion, ExposureSummary exposureSummary, IList<ExposureInformation> exposureInformations)
        {
            _loggerService.Info("ExposureDetected: Legacy-V1");

            ExposureConfiguration.GoogleExposureConfiguration configurationV1 = exposureConfiguration.GoogleExposureConfig;

            _ = Task.Run(async() =>
            {
                bool isNewExposureDetected = _userDataRepository.AppendExposureData(
                    exposureSummary,
                    exposureInformations.ToList(),
                    configurationV1.MinimumRiskScore
                    );

                if (isNewExposureDetected)
                {
                    _ = _localNotificationService.ShowExposureNotificationAsync();
                }
                else
                {
                    _loggerService.Info($"MatchedKeyCount: {exposureSummary.MatchedKeyCount}, but no new exposure detected");
                }

                await _exposureDataCollectServer.UploadExposureDataAsync(
                    exposureConfiguration,
                    DeviceInfo.Model,
                    enVersion,
                    exposureSummary, exposureInformations
                    );
            });
        }

        public void ExposureNotDetected(ExposureConfiguration exposureConfiguration, string enVersion)
        {
            _loggerService.Info("ExposureNotDetected");

            _ = Task.Run(async () =>
            {
                await _exposureDataCollectServer.UploadExposureDataAsync(
                    exposureConfiguration,
                    DeviceInfo.Model,
                    enVersion
                    );
            });
        }
    }
}