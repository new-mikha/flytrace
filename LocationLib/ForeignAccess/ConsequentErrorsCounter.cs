/******************************************************************************
 * Flytrace, online viewer for GPS trackers.
 * Copyright (C) 2011-2014 Mikhail Karmazin
 * 
 * This file is part of Flytrace.
 * 
 * Flytrace is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as
 * published by the Free Software Foundation, either version 3 of the
 * License, or (at your option) any later version.
 * 
 * Flytrace is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with Flytrace.  If not, see <http://www.gnu.org/licenses/>.
 *****************************************************************************/

namespace FlyTrace.LocationLib.ForeignAccess
{
  /// <summary>
  /// Sometimes request to a foreign server might fail, and this should be reported to the log.
  /// But such request failures are out of our control, so it considered as acceptable as soon as 
  /// there are not too much of them. This class holds counters for number of consequent errors
  /// without a single request success for different errors. If a number is less than the threshold,
  /// then the failure reported as Warn, otherwise it's reported as Error. This approach allows
  /// to avoid flooding logs (e.g. SmtpAppender) with generally useless error reports.
  /// </summary>
  public class ConsequentErrorsCounter
  {
    public readonly ThresholdCounter RequestsErrorsCounter;

    public readonly ThresholdCounter TimedOutRequestsCounter;

    public ConsequentErrorsCounter(
      int requestsErrorsThresold,
      int timedOutRequestsThresold
      )
    {
      RequestsErrorsCounter = new ThresholdCounter( requestsErrorsThresold );
      TimedOutRequestsCounter = new ThresholdCounter( timedOutRequestsThresold );
    }
  }
}
