﻿// Copyright (c) Microsoft Corporation.  All rights reserved.
// MIT License
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the ""Software""), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using Hl7.Fhir.Model;
using Microsoft.HealthVault.Fhir.Constants;
using Microsoft.HealthVault.ItemTypes;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Microsoft.HealthVault.Fhir.UnitTests")]

namespace Microsoft.HealthVault.Fhir.Codings
{
    internal class HealthVaultCodesToFhir
    {
        public static CodeableConcept ConvertCodableValueToFhir(CodableValue codableValue)
        {
            return new CodeableConcept
            {
                Text = codableValue.Text,
                Coding = ConvertCodableValueToFhir(codableValue, null)
            };
        }
        /// <summary>
        /// This function converts codable values into codings for FHIR. It takes a list of codings and returns it after
        /// adding the new codings. 
        /// </summary>
        /// <param name="codableValue">The list of codable values to convert</param>
        /// <param name="fhirCodes">The initial list of codings, this list will be modified to include the new ones</param>
        /// <returns>The updated list of fhir codes</returns>
        public static List<Coding> ConvertCodableValueToFhir(CodableValue codableValue, List<Coding> fhirCodes)
        {
            if(fhirCodes == null)
            {
                fhirCodes = new List<Coding>();
            }

            if (codableValue != null)
            {
                foreach (var code in codableValue)
                {
                    if (!string.IsNullOrWhiteSpace(code.Family) && code.Family.Equals(HealthVaultVocabularies.Wc, StringComparison.OrdinalIgnoreCase))
                    {                        
                        ConvertValueToFhir(code.Value, fhirCodes, code.VocabularyName, VocabularyUris.HealthVaultVocabulariesUri, code.Version, codableValue.Text);
                    }
                    else if (!string.IsNullOrWhiteSpace(code.Family) && (code.Family.Equals(HealthVaultVocabularies.RxNorm, StringComparison.OrdinalIgnoreCase) || code.Family.Equals(HealthVaultVocabularies.Dmd, StringComparison.OrdinalIgnoreCase)))
                    {
                        ConvertValueToFhir(code.Value, fhirCodes, code.VocabularyName, VocabularyUris.HealthVaultVocabulariesUri + code.Family, code.Version, codableValue.Text);
                    }
                    // If family is a well formed URI and it is a vocab name "fhir", the vocab name must be ignored
                    else if(!string.IsNullOrWhiteSpace(code.VocabularyName) && code.VocabularyName.Equals(HealthVaultVocabularies.Fhir, StringComparison.OrdinalIgnoreCase) && Uri.IsWellFormedUriString(code.Family, UriKind.Absolute))
                    {
                        ConvertValueToFhir(code.Value, fhirCodes, null, code.Family, code.Version, codableValue.Text);
                    }
                    else
                    {
                        ConvertValueToFhir(code.Value, fhirCodes, code.VocabularyName, code.Family, code.Version, codableValue.Text);
                    }
                }
            }

            return fhirCodes;
        }
        
        /// <summary>
        /// Converts a value from FHIR and adds it to the list of codings passed on the function
        /// </summary>
        /// <param name="value">The value to add as a coding</param>
        /// <param name="fhirCodes">The original list of codings</param>
        /// <param name="vocabName">The name of the vocabulary</param>
        /// <param name="system">The system of the vocabulary, default is HealthVault</param>
        /// <param name="version">The version of the vocabulary</param>
        /// <param name="display">The display text for the vocabulary</param>
        /// <returns>The updated list of fhir codes</returns>
        public static List<Coding> ConvertValueToFhir(string value, List<Coding> fhirCodes, string vocabName = null, string system = VocabularyUris.HealthVaultVocabulariesUri, string version = null, string display = null)
        {
            if (fhirCodes == null)
            {
                fhirCodes = new List<Coding>();
            }

            if (!string.IsNullOrWhiteSpace(vocabName))
            {
                value = string.Format(HealthVaultVocabularies.HealthVaultCodedValueFormat, vocabName, value);
            }

            fhirCodes.Add(new Coding
            {
                Code = value,
                Version = version,
                System = system,
                Display = display
            });

            return fhirCodes;
        }

        /// <summary>
        /// Returns a URL to be used for system that includes the vocabulary name and version
        /// </summary>
        /// <param name="vocabularyName">The name of the vocabulary</param>
        /// <param name="version">The version of the vocabulary</param>
        /// <returns></returns>
        public static string GetVocabularyUrl(string vocabularyName, string version)
        {
            return $"{VocabularyUris.HealthVaultVocabulariesUri}{vocabularyName}/{version}";
        }

        internal static CodeableConcept CreateVocabularyCodeableConcept(string code)
        {
            return new CodeableConcept { Coding = new List<Coding> { new Coding(VocabularyUris.HealthVaultVocabulariesUri, code) } };
        }

        internal static Dosage GetDosage(GeneralMeasurement dose, GeneralMeasurement frequency, CodableValue route)
        {
            var dosage = new Dosage();

            if (route != null)
            {
                dosage.Route = ConvertCodableValueToFhir(route);
            }
            if (frequency != null)
            {
                var repeatComponent = new Timing.RepeatComponent();
                StructuredMeasurement frequencyMeasurement = frequency.Structured.First();
                repeatComponent.Period = new decimal(frequencyMeasurement.Value);
                repeatComponent.PeriodUnit = GetPeriodUnitFromFromRecurrenceIntervals(frequencyMeasurement.Units);
                var timing = new Timing()
                {
                    Repeat = repeatComponent
                };

                dosage.Timing = timing;
            }
            if (dose != null)
            {
                var doseQuantity = new SimpleQuantity();
                StructuredMeasurement doseMeasurement = dose.Structured.First();
                doseQuantity.Value = new decimal(doseMeasurement.Value);
                doseQuantity.Unit = doseMeasurement.Units.Text;
                CodedValue doseUnit = doseMeasurement.Units.First();
                doseQuantity.System = GetVocabularyUrl(doseUnit.VocabularyName,doseUnit.Version);
                doseQuantity.Code = doseUnit.Value;
                dosage.Dose = doseQuantity;
            }

            return dosage;
        }

        private static Timing.UnitsOfTime? GetPeriodUnitFromFromRecurrenceIntervals(CodableValue units)
        {
            Func<CodedValue, bool> recurrenceIntervalsPredicate =
                coded => coded.VocabularyName == "recurrence-intervals";
            if (units.Any(recurrenceIntervalsPredicate))
            {
                var coded = units.First(recurrenceIntervalsPredicate);
                switch (coded.Value)
                {
                    case "second":
                        return Timing.UnitsOfTime.S;
                    case "minute":
                        return Timing.UnitsOfTime.Min;
                    case "hour":
                        return Timing.UnitsOfTime.H;
                    case "day":
                        return Timing.UnitsOfTime.D;
                    case "week":
                        return Timing.UnitsOfTime.Wk;
                    case "month":
                        return Timing.UnitsOfTime.Mo;
                    case "year":
                        return Timing.UnitsOfTime.A;
                    default:
                        return null;
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
