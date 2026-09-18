using System.Globalization;
using System.Text.Json;
using StockLentes.Models;
namespace StockLentes.Services;

// Strings intentionally retain unparseable values for review and future import.
public class LensEntry {
 public string? Code {get;set;}
 public string? Name {get;set;}
 public int? ProductId {get;set;}
 public string? Eye {get;set;}="OD";
 public string? Section {get;set;}="LEJOS";
 public string? Pair {get;set;}="1";
 public string? Sphere {get;set;}
 public string? Cylinder {get;set;}
 public string? Axis {get;set;}
 public string? Add {get;set;}
 public string? Base {get;set;}
 public List<SectionEntry>? Sections {get;set;}
 public static LensEntry Read(OrderLens lens,bool original=false){
  var json=original?lens.OriginalInputJson:lens.RawInputJson;
  if(!string.IsNullOrEmpty(json))return JsonSerializer.Deserialize<LensEntry>(json)??new();
  return new(){Code=lens.OriginalProductCode,Name=lens.OriginalProductName,ProductId=lens.RequestedProductId,
   Eye=lens.Eye,Section=lens.PairType,Pair=lens.PairNumber.ToString(CultureInfo.InvariantCulture),
   Sphere=Text(lens.Sphere100),Cylinder=Text(lens.Cylinder100),Axis=lens.Axis?.ToString(CultureInfo.InvariantCulture),Add=Text(lens.Add100),Base=Text(lens.Base100)};
 }
 public static string? Text(int? x)=>x.HasValue?(x.Value/100m).ToString("0.00",CultureInfo.InvariantCulture):null;
 public static int? Grade(string? raw,string name,List<string> issues){
  if(string.IsNullOrWhiteSpace(raw))return null;
  if(decimal.TryParse(raw.Trim().Replace(',','.'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out var value)&&value>=-100&&value<=100&&value*100==decimal.Truncate(value*100))return (int)(value*100);
  issues.Add(name+" no interpretable");return null;
 }
}
public record SectionEntry(string Section,string? Sphere,string? Cylinder,string? Axis);
