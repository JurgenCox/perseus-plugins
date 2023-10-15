using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BaseLibS.Api.Image;
using BaseLibS.Util;
namespace PerseusApi.Image{
	[Serializable]
	public class MriMetadata : ICloneable, IImageMetadata{
		/// <summary>
		/// Information that could be read in from the nifti header. 
		/// </summary>
		public NiftiHeader NiftiHeader{ get; set; }
		/// <summary>
		/// information about BIDS entities. Thus, should clearly identify each run. 
		/// </summary>
		public BIDSData BIDSData{ get; set; }

		// unit information
		public float xyz_unit{ get; set; }
		public float t_unit{ get; set; }
		/// <summary>
		/// Rigid Body Transformation matrices for reslicing (not to confuse with Head Motion Correction parameters. Matrix is calculated from these). 
		/// indices [t,row,column] where t = timesteps, row = {0,1,2,3}, column = {0,1,2,3}. Each timestep has one matrix. 
		/// To understand matrix see https://brainder.org/2012/09/23/the-nifti-file-format section "Orientation information - Method 3"
		/// </summary>
		public double[,,] RigidBodyTransformation{ get; set; }
		/// <summary>
		/// Deformation field matrix for normalisation. 5-D nifti file, it is used to modify fMRI or anatomic images to match the tissue probability map.
		/// The file is the result of normalisation estimate or segmentation
		/// </summary>
		public float[,][,,] DeformationField{ get; set; }
		/// <summary>
		/// Nifti header for the deformation field
		/// </summary>
		public NiftiHeader DefFieldHeader{ get; set; }
		/// <summary>
		/// Information about events as read in from .csv file. 
		/// indices [i,j] : i = {0,1,...,n-1} indices of events, n = number of events; j = {0,1,2} where 0 = onset, 1 = duration, 2 = trial type
		/// unit is seconds (as specified by the BIDS specifications). 
		/// </summary>
		public string[,] Events{ get; set; }

		// information from JSON file (in BIDS specification, see https://bids-specification.readthedocs.io/en/stable/04-modality-specific-files/01-magnetic-resonance-imaging-data.html)

		// scanner hardware information
		public string Manufacturer{ get; set; }
		public string ManufacturersModelName{ get; set; }
		public string DeviceSerialNumber{ get; set; }
		public string StationName{ get; set; }
		public string SoftwareVersions{ get; set; }
		public double? MagneticFieldStrength{ get; set; }
		public string ReceiveCoilName{ get; set; }
		public string ReceiveCoilActiveElements{ get; set; }
		public string GradientSetType{ get; set; }
		public string MRTransmitCoilSequence{ get; set; }
		public string MatrixCoilMode{ get; set; }
		public string CoilCombinationMethod{ get; set; }

		// sequence specifics
		public string PulseSequenceType{ get; set; }
		public string[] ScanningSequence{ get; set; }
		public string[] SequenceVariant{ get; set; }
		public string[] ScanOptions{ get; set; }
		public string SequenceName{ get; set; }
		public string PulseSequenceDetails{ get; set; }
		public bool? NonlinearGradientCorrection{ get; set; }
		public string MRAcquisitionType{ get; set; }
		public bool? MTState{ get; set; }
		public double? MTOffsetFrequency{ get; set; }
		public double? MTPulseBandwidth{ get; set; }
		public double? MTNumberOfPulses{ get; set; }
		public string MTPulseShape{ get; set; }
		public double? MTPulseDuration{ get; set; }
		public bool? SpoilingState{ get; set; }
		public string SpoilingType{ get; set; }
		public double? SpoilingRFPhaseIncrement{ get; set; }
		public double? SpoilingGradientMoment{ get; set; }
		public double? SpoilingGradientDuration{ get; set; }

		// in-plane spatial encoding
		public double[] NumberShots{ get; set; }
		public double? ParallelReductionFactorInPlane{ get; set; }
		public string ParallelAcquisitionTechnique{ get; set; }
		public double? PartialFourier{ get; set; }
		public string PartialFourierDirection{ get; set; }
		public string PhaseEncodingDirection{ get; set; }
		public double? EffectiveEchoSpacing{ get; set; }
		public double? TotalReadoutTime{ get; set; }
		public double? MixingTime{ get; set; }

		// timing parameters
		public double[] EchoTime{ get; set; }
		public double? InversionTime{ get; set; }
		public double[] SliceTiming{ get; set; }
		public string SliceEncodingDirection{ get; set; }
		public double? DwellTime{ get; set; }

		// RF & Contrast
		public double[] FlipAngle{ get; set; }
		public bool NegativeContrast{ get; set; }

		// slice acceleration
		public double? MultibandAccelerationFactor{ get; set; }

		// anatomical landmarks
		// public object AnatomicalLandmarkCoordinates { get; set; } // TODO I don't know yet what type this should be

		// institution information
		public string InstitutionName{ get; set; }
		public string InstitutionAddress{ get; set; }
		public string InstitutionalDepartmentName{ get; set; }

		// metadata information for anatomical images
		public string ContrastBolusIngredient{ get; set; }
		public double? RepetitionTimeExcitation{ get; set; }
		public double[] RepetitionTimePreparation{ get; set; }

		// metadata information for task images (fMRI, rsfMRI, ...)
		// required
		public double? RepetitionTime{ get; set; }
		public double[] VolumeTiming{ get; set; }
		public string TaskName{ get; set; }
		// recommended
		public int? NumberOfVolumesDiscardedByScanner{ get; set; }
		public int? NumberOfVolumesDiscardedByUser{ get; set; }
		public double? DelayTime{ get; set; }
		public double? AcquisitionDuration{ get; set; }
		public double? DelayAfterTrigger{ get; set; }
		public string Instructions{ get; set; }
		public string TaskDescription{ get; set; }
		public string CogAtlasID{ get; set; }
		public string CogPOID{ get; set; }

		// metadata information for dwi images
		public string MultipartID{ get; set; }

		// other metadata fields were not included yet, can be found at link above. 
		public MriMetadata(){
		}
		public MriMetadata(string niftiPath){
			ReadNiftiHeader(niftiPath);
		}
		public MriMetadata(BinaryReader reader){
			NiftiHeader = new NiftiHeader(reader);
			BIDSData = new BIDSData(reader);
			xyz_unit = reader.ReadSingle();
			t_unit = reader.ReadSingle();
			RigidBodyTransformation = FileUtils.Read3DDoubleArray1(reader);
			DeformationField = FileUtils.Read5DFloatArray2(reader);
			DefFieldHeader = new NiftiHeader(reader);
			Events = FileUtils.Read2DStringArray2(reader);
			Manufacturer = reader.ReadString();
			ManufacturersModelName = reader.ReadString();
			DeviceSerialNumber = reader.ReadString();
			StationName = reader.ReadString();
			SoftwareVersions = reader.ReadString();
			MagneticFieldStrength = FileUtils.ReadNullableDouble(reader);
			ReceiveCoilName = reader.ReadString();
			ReceiveCoilActiveElements = reader.ReadString();
			GradientSetType = reader.ReadString();
			MRTransmitCoilSequence = reader.ReadString();
			MatrixCoilMode = reader.ReadString();
			CoilCombinationMethod = reader.ReadString();
			PulseSequenceType = reader.ReadString();
			ScanningSequence = FileUtils.ReadStringArray(reader);
			SequenceVariant = FileUtils.ReadStringArray(reader);
			ScanOptions = FileUtils.ReadStringArray(reader);
			SequenceName = reader.ReadString();
			PulseSequenceDetails = reader.ReadString();
			NonlinearGradientCorrection = FileUtils.ReadNullableBoolean(reader);
			MRAcquisitionType = reader.ReadString();
			MTState = FileUtils.ReadNullableBoolean(reader);
			MTOffsetFrequency = FileUtils.ReadNullableDouble(reader);
			MTPulseBandwidth = FileUtils.ReadNullableDouble(reader);
			MTNumberOfPulses = FileUtils.ReadNullableDouble(reader);
			MTPulseShape = reader.ReadString();
			MTPulseDuration = FileUtils.ReadNullableDouble(reader);
			SpoilingState = FileUtils.ReadNullableBoolean(reader);
			SpoilingType = reader.ReadString();
			SpoilingRFPhaseIncrement = FileUtils.ReadNullableDouble(reader);
			SpoilingGradientMoment = FileUtils.ReadNullableDouble(reader);
			SpoilingGradientDuration = FileUtils.ReadNullableDouble(reader);
			NumberShots = FileUtils.ReadDoubleArray(reader);
			ParallelReductionFactorInPlane = FileUtils.ReadNullableDouble(reader);
			ParallelAcquisitionTechnique = reader.ReadString();
			PartialFourier = FileUtils.ReadNullableDouble(reader);
			PartialFourierDirection = reader.ReadString();
			PhaseEncodingDirection = reader.ReadString();
			EffectiveEchoSpacing = FileUtils.ReadNullableDouble(reader);
			TotalReadoutTime = FileUtils.ReadNullableDouble(reader);
			MixingTime = FileUtils.ReadNullableDouble(reader);
			EchoTime = FileUtils.ReadDoubleArray(reader);
			InversionTime = FileUtils.ReadNullableDouble(reader);
			SliceTiming = FileUtils.ReadDoubleArray(reader);
			SliceEncodingDirection = reader.ReadString();
			DwellTime = FileUtils.ReadNullableDouble(reader);
			FlipAngle = FileUtils.ReadDoubleArray(reader);
			NegativeContrast = reader.ReadBoolean();
			MultibandAccelerationFactor = FileUtils.ReadNullableDouble(reader);
			InstitutionName = reader.ReadString();
			InstitutionAddress = reader.ReadString();
			InstitutionalDepartmentName = reader.ReadString();
			ContrastBolusIngredient = reader.ReadString();
			RepetitionTimeExcitation = FileUtils.ReadNullableDouble(reader);
			RepetitionTimePreparation = FileUtils.ReadDoubleArray(reader);
			RepetitionTime = FileUtils.ReadNullableDouble(reader);
			VolumeTiming = FileUtils.ReadDoubleArray(reader);
			TaskName = reader.ReadString();
			NumberOfVolumesDiscardedByScanner = FileUtils.ReadNullableInt32(reader);
			NumberOfVolumesDiscardedByUser = FileUtils.ReadNullableInt32(reader);
			DelayTime = FileUtils.ReadNullableDouble(reader);
			AcquisitionDuration = FileUtils.ReadNullableDouble(reader);
			DelayAfterTrigger = FileUtils.ReadNullableDouble(reader);
			Instructions = reader.ReadString();
			TaskDescription = reader.ReadString();
			CogAtlasID = reader.ReadString();
			CogPOID = reader.ReadString();
			MultipartID = reader.ReadString();
		}
		public void Write(BinaryWriter writer){
			NiftiHeader.Write(writer);
			BIDSData.Write(writer);
			writer.Write(xyz_unit);
			writer.Write(t_unit);
			FileUtils.Write(RigidBodyTransformation, writer);
			FileUtils.Write(DeformationField, writer);
			DefFieldHeader.Write(writer);
			FileUtils.Write(Events, writer);
			writer.Write(Manufacturer);
			writer.Write(ManufacturersModelName);
			writer.Write(DeviceSerialNumber);
			writer.Write(StationName);
			writer.Write(SoftwareVersions);
			FileUtils.Write(MagneticFieldStrength, writer);
			FileUtils.Write(MagneticFieldStrength, writer);
			writer.Write(ReceiveCoilName);
			writer.Write(ReceiveCoilActiveElements);
			writer.Write(GradientSetType);
			writer.Write(MRTransmitCoilSequence);
			writer.Write(MatrixCoilMode);
			writer.Write(CoilCombinationMethod);
			writer.Write(PulseSequenceType);
			FileUtils.Write(ScanningSequence, writer);
			FileUtils.Write(SequenceVariant, writer);
			FileUtils.Write(ScanOptions, writer);
			writer.Write(SequenceName);
			writer.Write(PulseSequenceDetails);
			FileUtils.Write(NonlinearGradientCorrection, writer);
			writer.Write(MRAcquisitionType);
			FileUtils.Write(MTState, writer);
			FileUtils.Write(MTOffsetFrequency, writer);
			FileUtils.Write(MTPulseBandwidth, writer);
			FileUtils.Write(MTNumberOfPulses, writer);
			writer.Write(MTPulseShape);
			FileUtils.Write(MTPulseDuration, writer);
			FileUtils.Write(SpoilingState, writer);
			writer.Write(SpoilingType);
			FileUtils.Write(SpoilingRFPhaseIncrement, writer);
			FileUtils.Write(SpoilingGradientMoment, writer);
			FileUtils.Write(SpoilingGradientDuration, writer);
			FileUtils.Write(NumberShots, writer);
			FileUtils.Write(ParallelReductionFactorInPlane, writer);
			writer.Write(ParallelAcquisitionTechnique);
			FileUtils.Write(PartialFourier, writer);
			writer.Write(PartialFourierDirection);
			writer.Write(PhaseEncodingDirection);
			FileUtils.Write(EffectiveEchoSpacing, writer);
			FileUtils.Write(TotalReadoutTime, writer);
			FileUtils.Write(MixingTime, writer);
			FileUtils.Write(EchoTime, writer);
			FileUtils.Write(InversionTime, writer);
			FileUtils.Write(SliceTiming, writer);
			writer.Write(SliceEncodingDirection);
			FileUtils.Write(DwellTime, writer);
			FileUtils.Write(FlipAngle, writer);
			writer.Write(NegativeContrast);
			FileUtils.Write(MultibandAccelerationFactor, writer);
			writer.Write(InstitutionName);
			writer.Write(InstitutionAddress);
			writer.Write(InstitutionalDepartmentName);
			writer.Write(ContrastBolusIngredient);
			FileUtils.Write(RepetitionTimeExcitation, writer);
			FileUtils.Write(RepetitionTimePreparation, writer);
			FileUtils.Write(RepetitionTime, writer);
			FileUtils.Write(VolumeTiming, writer);
			writer.Write(TaskName);
			FileUtils.Write(NumberOfVolumesDiscardedByScanner, writer);
			FileUtils.Write(NumberOfVolumesDiscardedByUser, writer);
			FileUtils.Write(DelayTime, writer);
			FileUtils.Write(AcquisitionDuration, writer);
			FileUtils.Write(DelayAfterTrigger, writer);
			writer.Write(Instructions);
			writer.Write(TaskDescription);
			writer.Write(CogAtlasID);
			writer.Write(CogPOID);
			writer.Write(MultipartID);
		}
		/// <summary>
		/// Returns a wanted BIDS entity (i.e., "sub" or "run") if existing. 
		/// </summary>
		/// <param name="entityName"></param>
		/// <returns></returns>
		public string GetBIDSEntity(string entityName){
			if (BIDSData != null){
				try{
					return (string) typeof(BIDSData).GetProperty(entityName).GetValue(BIDSData);
				} catch{
					Console.WriteLine("This BIDS Entry does not exist.");
					return "unknown";
				}
			} else{
				throw new Exception("BIDS data not existing");
			}
		}
		/// <summary>
		/// Read in the information of a nifti header. 
		/// </summary>
		/// <param name="path"></param>
		public (bool, string) ReadNiftiHeader(string path){
			NiftiHeader = new NiftiHeader();
			return NiftiHeader.ReadNiftiHeader(path, this);
		}
		/// <summary>
		/// Returns the data that is stored in a nifti file. 
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		public float[][,,] GetDataFromNifti(string path){
			if (NiftiHeader != null){
				return NiftiHeader.GetData(path);
			} else{
				return null;
			}
		}
		public float[,,,][,,] GetDataFromNiftiBig(string path){
			if (NiftiHeader != null){
				return NiftiHeader.GetData2(path);
			} else{
				return null;
			}
		}

		//TODO writeNiftiBig far all nifti
		//public void WriteNiftiFileBig(string path, float[,,,][,,] data)
		//{
		//    if (NiftiHeader != null)
		//    {
		//        NiftiHeader.WriteNiftiFile2(path, data);
		//    }
		//}
		public void WriteNiftiFileBig(string path, float[,][,,] data){
			if (NiftiHeader != null){
				NiftiHeader.WriteNiftiFile5D(path, data);
			}
		}
		/// <summary>
		/// Write a nifti file filled with given data to a given path (works only if NiftiHeader is not null). 
		/// </summary>
		/// <param name="path"></param>
		/// <param name="data"></param>
		public void WriteNiftiFile(string path, float[][,,] data){
			if (NiftiHeader != null){
				NiftiHeader.WriteNiftiFile(path, data);
			}
		}
		/// <summary>
		/// Decode the information encoded in a BIDS encoded path and read to BIDSData property. 
		/// </summary>
		/// <param name="path"></param>
		public void ReadBIDS(string path){
			BIDSData = new BIDSData(path);
		}
		/// <summary>
		/// Return only first timestep (one 3D image). Needed to make anatomical images. 
		/// </summary>
		/// <param name="olddata"></param>
		/// <returns></returns>
		public float[,,] ReduceData(float[][,,] olddata){
			if (NiftiHeader != null){
				return NiftiHeader.ReduceData(olddata);
			} else{
				return null;
			}
		}
		public float[,,] ReduceData(float[,,] olddata){
			return olddata;
		}
		/// <summary>
		/// Read events from a events.tsv file (path provided) and store in Events property. 
		/// </summary>
		/// <param name="path"></param>
		public List<string> ReadEvents(string path){
			List<string> eventNames = new List<string>();
			List<string> onset = new List<string>();
			List<string> duration = new List<string>();
			List<string> trialType = new List<string>();
			int o = -1;
			int d = -1;
			int t = -1;
			using (var reader = new StreamReader(path)){
				var line = reader.ReadLine();
				var values = line.Split('\t');
				for (int i = 0; i < values.Length; i++){
					switch (values[i]){
						case "onset":
							o = i;
							break;
						case "duration":
							d = i;
							break;
						case "trial_type":
							t = i;
							break;
					}
				}
				if (o != -1 && d != -1 && t == -1){
					while (!reader.EndOfStream){
						line = reader.ReadLine();
						values = line.Split('\t');
						onset.Add(values[o]);
						duration.Add(values[d]);
						trialType.Add(String.Empty);
					}
				} else if (o != -1 && d != -1 && t != -1){
					while (!reader.EndOfStream){
						line = reader.ReadLine();
						values = line.Split('\t');
						onset.Add(values[o]);
						duration.Add(values[d]);
						trialType.Add(values[t]);
					}
				} else{
					return null;
				}
			}
			Events = new string[onset.Count, 3];
			for (int i = 0; i < onset.Count; i++){
				Events[i, 0] = onset[i];
				Events[i, 1] = duration[i];
				Events[i, 2] = trialType[i];
				if (!eventNames.Contains(trialType[i])){
					eventNames.Add(trialType[i]);
				}
			}
			return eventNames;
		}
		/// <summary>
		/// Search for a events.tsv file that fits the BIDS path of a provided nifti file and store information in Events property if found. 
		/// </summary>
		/// <param name="niftiPath"></param>
		public List<string> SearchAndReadEvents(string niftiPath){
			string[] split = niftiPath.Split('_');
			string eventPath = niftiPath.Substring(0, niftiPath.Length - split[split.Length - 1].Length) + "events.tsv";
			List<string> eventNames = new List<string>();
			if (File.Exists(eventPath)){
				List<string> curEventNames = ReadEvents(eventPath);
				foreach (string name in curEventNames){
					if (!eventNames.Contains(name)){
						eventNames.Add(name);
					}
				}
			}
			if (eventNames.Count != 0){
				return eventNames;
			} else{
				return null;
			}
		}
		/// <summary>
		/// Fill the MRIMetadata object with the properties provided in a dictionary. 
		/// </summary>
		/// <param name="dict">Dictionary. K: property name, V: property content</param>        
		public void FillMetadataFromJSON(Dictionary<string, object> dict){
			foreach (KeyValuePair<string, object> entry in dict){
				PropertyInfo pi = this.GetType().GetProperty(entry.Key);

				// check if we can store this kind of information
				if (pi != null){
					// store if not an array
					if (pi.PropertyType == typeof(string)){
						if (!(entry.Value.GetType() == typeof(string))){
							throw new Exception("Error during JSON Parsing: Invalid Value (not string) for Key " +
							                    entry.Key);
						}
						pi.SetValue(this, Convert.ToString(entry.Value));
					} else if (pi.PropertyType == typeof(int?)){
						if (!(entry.Value.GetType() == typeof(Int64) | entry.Value.GetType() == typeof(Int32) |
						      entry.Value.GetType() == typeof(Int16) | entry.Value.GetType() == typeof(UInt32) |
						      entry.Value.GetType() == typeof(UInt16) | entry.Value.GetType() == typeof(UInt64))){
							throw new Exception(
								"Error during JSON Parsing: Invalid Value (not int or double) for Key " + entry.Key);
						}
						pi.SetValue(this, Convert.ToInt32(entry.Value));
					} else if (pi.PropertyType == typeof(bool?)){
						if (!(entry.Value.GetType() == typeof(bool))){
							throw new Exception("Error during JSON Parsing: Invalid Value (not boolean) for Key " +
							                    entry.Key);
						}
						pi.SetValue(this, Convert.ToBoolean(entry.Value));
					} else if (pi.PropertyType == typeof(double?)){
						if (!(entry.Value.GetType() == typeof(Int64) | entry.Value.GetType() == typeof(Int32) |
						      entry.Value.GetType() == typeof(Int16) | entry.Value.GetType() == typeof(UInt32) |
						      entry.Value.GetType() == typeof(UInt16) | entry.Value.GetType() == typeof(UInt64) |
						      entry.Value.GetType() == typeof(Double) | entry.Value.GetType() == typeof(Single))){
							throw new Exception(
								"Error during JSON Parsing: Invalid Value (not int or double) for Key " + entry.Key);
						}
						pi.SetValue(this, Convert.ToDouble(entry.Value));
					}

					// double arrays are vague, can also be just double
					else if (pi.PropertyType == typeof(double[])){
						if (entry.Value.GetType() == typeof(Int64) | entry.Value.GetType() == typeof(Int32) |
						    entry.Value.GetType() == typeof(Int16) | entry.Value.GetType() == typeof(UInt32) |
						    entry.Value.GetType() == typeof(UInt16) | entry.Value.GetType() == typeof(UInt64) |
						    entry.Value.GetType() == typeof(Double) | entry.Value.GetType() == typeof(Single)){
							pi.SetValue(this, new double[]{Convert.ToDouble(entry.Value)});
						} else{
							try{
								pi.SetValue(this, ((Newtonsoft.Json.Linq.JArray) entry.Value).ToObject<double[]>());
							} catch{
								throw new Exception("Error during JSON Parsing: Didn't understand Value of Key " +
								                    entry.Key);
							}
						}
					}
					// string arrays are vague, can also be just string
					else if (pi.PropertyType == typeof(string[])){
						if (entry.Value.GetType() == typeof(string)){
							pi.SetValue(this, new string[]{Convert.ToString(entry.Value)});
						} else{
							try{
								pi.SetValue(this, ((Newtonsoft.Json.Linq.JArray) entry.Value).ToObject<string[]>());
							} catch{
								throw new Exception("Error during JSON Parsing: Didn't understand Value of Key " +
								                    entry.Key);
							}
						}
					}
				}
			}
		}
		/// <summary>
		/// Search for a bold.json file that fits the BIDS path of a provided nifti file and read information if found. 
		/// </summary>
		/// <param name="niftiPath">path to nifti file, indicating the expected json path</param>
		public void SearchAndReadMetadata(string niftiPath){
			string[] split = niftiPath.Split('_');
			string jsonPath = niftiPath.Substring(0, niftiPath.Length - split[split.Length - 1].Length) + "bold.json";
			if (File.Exists(jsonPath)){
				Dictionary<string, object> dict = Basics.ReadMetadataFromJSON(jsonPath);
				FillMetadataFromJSON(dict);
			}
		}
		public object Clone(){
			if (this.DeformationField != null){
				float[,][,,] newDeformationField = new float[1, this.DeformationField.Length][,,];
				for (int i = 0; i < this.DeformationField.Length; i++){
					newDeformationField[0, i] = (float[,,]) DeformationField[0, i]?.Clone();
				}
				MriMetadata clone = new MriMetadata(){
					NiftiHeader = (NiftiHeader) NiftiHeader?.Clone(),
					xyz_unit = xyz_unit,
					t_unit = t_unit,
					RigidBodyTransformation = (double[,,]) RigidBodyTransformation?.Clone(),
					DeformationField = newDeformationField,
					Events = (string[,]) Events?.Clone(),
					BIDSData = (BIDSData) BIDSData?.Clone(),
					DefFieldHeader = (NiftiHeader) DefFieldHeader?.Clone(),
					Manufacturer = Manufacturer,
					ManufacturersModelName = ManufacturersModelName,
					DeviceSerialNumber = DeviceSerialNumber,
					StationName = StationName,
					SoftwareVersions = SoftwareVersions,
					MagneticFieldStrength = MagneticFieldStrength,
					ReceiveCoilName = ReceiveCoilName,
					ReceiveCoilActiveElements = ReceiveCoilActiveElements,
					GradientSetType = GradientSetType,
					MRTransmitCoilSequence = MRTransmitCoilSequence,
					MatrixCoilMode = MatrixCoilMode,
					CoilCombinationMethod = CoilCombinationMethod,
					PulseSequenceType = PulseSequenceType,
					ScanningSequence = (string[]) ScanningSequence?.Clone(),
					SequenceVariant = (string[]) SequenceVariant?.Clone(),
					ScanOptions = (string[]) ScanOptions?.Clone(),
					SequenceName = SequenceName,
					PulseSequenceDetails = PulseSequenceDetails,
					NonlinearGradientCorrection = NonlinearGradientCorrection,
					MRAcquisitionType = MRAcquisitionType,
					MTState = MTState,
					MTOffsetFrequency = MTOffsetFrequency,
					MTPulseBandwidth = MTPulseBandwidth,
					MTNumberOfPulses = MTNumberOfPulses,
					MTPulseShape = MTPulseShape,
					MTPulseDuration = MTPulseDuration,
					SpoilingState = SpoilingState,
					SpoilingType = SpoilingType,
					SpoilingRFPhaseIncrement = SpoilingRFPhaseIncrement,
					SpoilingGradientMoment = SpoilingGradientMoment,
					SpoilingGradientDuration = SpoilingGradientDuration,
					NumberShots = (double[]) NumberShots?.Clone(),
					ParallelReductionFactorInPlane = ParallelReductionFactorInPlane,
					ParallelAcquisitionTechnique = ParallelAcquisitionTechnique,
					PartialFourier = PartialFourier,
					PartialFourierDirection = PartialFourierDirection,
					PhaseEncodingDirection = PhaseEncodingDirection,
					EffectiveEchoSpacing = EffectiveEchoSpacing,
					TotalReadoutTime = TotalReadoutTime,
					MixingTime = MixingTime,
					EchoTime = (double[]) EchoTime?.Clone(),
					InversionTime = InversionTime,
					SliceTiming = (double[]) SliceTiming?.Clone(),
					SliceEncodingDirection = SliceEncodingDirection,
					DwellTime = DwellTime,
					FlipAngle = (double[]) FlipAngle?.Clone(),
					NegativeContrast = NegativeContrast,
					MultibandAccelerationFactor = MultibandAccelerationFactor,
					InstitutionName = InstitutionName,
					InstitutionAddress = InstitutionAddress,
					InstitutionalDepartmentName = InstitutionalDepartmentName,
					ContrastBolusIngredient = ContrastBolusIngredient,
					RepetitionTimeExcitation = RepetitionTimeExcitation,
					RepetitionTimePreparation = (double[]) RepetitionTimePreparation?.Clone(),
					RepetitionTime = RepetitionTime,
					VolumeTiming = (double[]) VolumeTiming?.Clone(),
					TaskName = TaskName,
					NumberOfVolumesDiscardedByScanner = NumberOfVolumesDiscardedByScanner,
					NumberOfVolumesDiscardedByUser = NumberOfVolumesDiscardedByUser,
					DelayTime = DelayTime,
					AcquisitionDuration = AcquisitionDuration,
					DelayAfterTrigger = DelayAfterTrigger,
					Instructions = Instructions,
					TaskDescription = TaskDescription,
					CogAtlasID = CogAtlasID,
					CogPOID = CogPOID,
					MultipartID = MultipartID,
				};
				return clone;
			} else{
				MriMetadata clone = new MriMetadata(){
					NiftiHeader = (NiftiHeader) NiftiHeader?.Clone(),
					xyz_unit = xyz_unit,
					t_unit = t_unit,
					RigidBodyTransformation = (double[,,]) RigidBodyTransformation?.Clone(),
					Events = (string[,]) Events?.Clone(),
					BIDSData = (BIDSData) BIDSData?.Clone(),
					DefFieldHeader = (NiftiHeader) DefFieldHeader?.Clone(),
					Manufacturer = Manufacturer,
					ManufacturersModelName = ManufacturersModelName,
					DeviceSerialNumber = DeviceSerialNumber,
					StationName = StationName,
					SoftwareVersions = SoftwareVersions,
					MagneticFieldStrength = MagneticFieldStrength,
					ReceiveCoilName = ReceiveCoilName,
					ReceiveCoilActiveElements = ReceiveCoilActiveElements,
					GradientSetType = GradientSetType,
					MRTransmitCoilSequence = MRTransmitCoilSequence,
					MatrixCoilMode = MatrixCoilMode,
					CoilCombinationMethod = CoilCombinationMethod,
					PulseSequenceType = PulseSequenceType,
					ScanningSequence = (string[]) ScanningSequence?.Clone(),
					SequenceVariant = (string[]) SequenceVariant?.Clone(),
					ScanOptions = (string[]) ScanOptions?.Clone(),
					SequenceName = SequenceName,
					PulseSequenceDetails = PulseSequenceDetails,
					NonlinearGradientCorrection = NonlinearGradientCorrection,
					MRAcquisitionType = MRAcquisitionType,
					MTState = MTState,
					MTOffsetFrequency = MTOffsetFrequency,
					MTPulseBandwidth = MTPulseBandwidth,
					MTNumberOfPulses = MTNumberOfPulses,
					MTPulseShape = MTPulseShape,
					MTPulseDuration = MTPulseDuration,
					SpoilingState = SpoilingState,
					SpoilingType = SpoilingType,
					SpoilingRFPhaseIncrement = SpoilingRFPhaseIncrement,
					SpoilingGradientMoment = SpoilingGradientMoment,
					SpoilingGradientDuration = SpoilingGradientDuration,
					NumberShots = (double[]) NumberShots?.Clone(),
					ParallelReductionFactorInPlane = ParallelReductionFactorInPlane,
					ParallelAcquisitionTechnique = ParallelAcquisitionTechnique,
					PartialFourier = PartialFourier,
					PartialFourierDirection = PartialFourierDirection,
					PhaseEncodingDirection = PhaseEncodingDirection,
					EffectiveEchoSpacing = EffectiveEchoSpacing,
					TotalReadoutTime = TotalReadoutTime,
					MixingTime = MixingTime,
					EchoTime = (double[]) EchoTime?.Clone(),
					InversionTime = InversionTime,
					SliceTiming = (double[]) SliceTiming?.Clone(),
					SliceEncodingDirection = SliceEncodingDirection,
					DwellTime = DwellTime,
					FlipAngle = (double[]) FlipAngle?.Clone(),
					NegativeContrast = NegativeContrast,
					MultibandAccelerationFactor = MultibandAccelerationFactor,
					InstitutionName = InstitutionName,
					InstitutionAddress = InstitutionAddress,
					InstitutionalDepartmentName = InstitutionalDepartmentName,
					ContrastBolusIngredient = ContrastBolusIngredient,
					RepetitionTimeExcitation = RepetitionTimeExcitation,
					RepetitionTimePreparation = (double[]) RepetitionTimePreparation?.Clone(),
					RepetitionTime = RepetitionTime,
					VolumeTiming = (double[]) VolumeTiming?.Clone(),
					TaskName = TaskName,
					NumberOfVolumesDiscardedByScanner = NumberOfVolumesDiscardedByScanner,
					NumberOfVolumesDiscardedByUser = NumberOfVolumesDiscardedByUser,
					DelayTime = DelayTime,
					AcquisitionDuration = AcquisitionDuration,
					DelayAfterTrigger = DelayAfterTrigger,
					Instructions = Instructions,
					TaskDescription = TaskDescription,
					CogAtlasID = CogAtlasID,
					CogPOID = CogPOID,
					MultipartID = MultipartID,
				};
				return clone;
			}
		}
	}
}