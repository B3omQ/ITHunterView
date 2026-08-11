using Microsoft.EntityFrameworkCore.Migrations;

using System;
using System.Text;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJdMatchingV3SemanticPrompt : Migration
    {
        private const string MatchingContentBase64 = "WW91IGFyZSBhIHByb2Zlc3Npb25hbCBJVCByZWNydWl0bWVudCBhc3Npc3RhbnQuIFlvdXIgdGFzayBpcyB0byBldmFsdWF0ZSBob3cgd2VsbCB0aGUgY2FuZGlkYXRlIENWIHN1cHBvcnRzIGVhY2ggc3VwcGxpZWQgSkQgcmVxdWlyZW1lbnQgaXRlbS4KCkV2ZXJ5IHRleHR1YWwgZmllbGQgaW4gdGhlIHJlc3BvbnNlIG11c3QgYmUgd3JpdHRlbiBpbiBFbmdsaXNoLiBVc2Ugb25seSB0aGUgc3VwcGxpZWQgQ1YgYW5kIEpEIHJlcXVpcmVtZW50IGRhdGEuIE5ldmVyIGludmVudCBhIHNraWxsLCBkdXJhdGlvbiwgcXVhbGlmaWNhdGlvbiwgcXVvdGF0aW9uLCBzZWN0aW9uLCByZXNwb25zaWJpbGl0eSwgcHJvamVjdCwgb3V0Y29tZSwgb3IgZW1wbG95bWVudCBjb250ZXh0LgoKSU5QVVQgREFUQQotLS0gU1RBUlQgQ1YgLS0tCltDVl9URVhUXQotLS0gRU5EIENWIC0tLQoKLS0tIFNUQVJUIEpEIFJFUVVJUkVNRU5UUyAtLS0KW1BBUlNFRF9KRF9SRVFVSVJFTUVOVFNdCi0tLSBFTkQgSkQgUkVRVUlSRU1FTlRTIC0tLQoKVEFTSwoxLiBFdmFsdWF0ZSBldmVyeSBzdXBwbGllZCByZXF1aXJlbWVudCBpdGVtIGluZGVwZW5kZW50bHksIGluY2x1ZGluZyBldmVyeSBhbHRlcm5hdGl2ZSBpbnNpZGUgb25lX29mIGFuZCBhdF9sZWFzdF9uIGdyb3Vwcy4KMi4gUmV0dXJuIHRoZSBleGFjdCByZXFJZCBzdXBwbGllZCBmb3IgdGhhdCBpdGVtLiBOZXZlciBjcmVhdGUsIHJld3JpdGUsIG1lcmdlLCBvbWl0LCBvciByZXVzZSBhbiBJRC4KMy4gU2VsZWN0IGV4YWN0bHkgb25lIGFwcHJvdmVkIGhhbmRsZXJDb2RlIGZvciB0aGUgaXRlbSdzIHN1cHBsaWVkIGNhdGVnb3J5LiBEbyBub3QgcmV0dXJuIGEgbnVtZXJpYyBzY29yZTsgdGhlIGFwcGxpY2F0aW9uIG1hcHMgaGFuZGxlckNvZGUgdG8gaXRzIGZpeGVkIHNjb3JlLgo0LiBSZXNwZWN0IHRoZSBjb21wbGV0ZSBtZWFuaW5nIG9mIHRoZSBzdXBwbGllZCByZXF1aXJlbWVudCwgaW5jbHVkaW5nIG1pbmltdW0geWVhcnMsIHByb2Zlc3Npb25hbCBvciBoYW5kcy1vbiBjb250ZXh0LCBhbHRlcm5hdGl2ZXMsIGRlZ3JlZSBzdGF0dXMsIGNlcnRpZmljYXRpb24gdGhyZXNob2xkcywgbGFuZ3VhZ2UgY29udGV4dCwgYW5kIGRvbWFpbiBkZXB0aC4KNS4gRG8gbm90IGRlY2lkZSBncm91cCBhZ2dyZWdhdGlvbi4gVGhlIGFwcGxpY2F0aW9uIGFwcGxpZXMgYWxsX29mLCBvbmVfb2YsIGFuZCBhdF9sZWFzdF9uIGFmdGVyIGV2ZXJ5IGl0ZW0gaGFzIGJlZW4gZXZhbHVhdGVkLgo2LiBGb3IgZWFjaCBpdGVtLCBleHBsYWluIGluIGRldGFpbDogd2hhdCB0aGUgSkQgcmVxdWlyZXMsIHdoYXQgcmVsZXZhbnQgQ1YgZXZpZGVuY2UgZXhpc3RzIG9yIGlzIGFic2VudCwgYW5kIHdoeSB0aGUgc2VsZWN0ZWQgaGFuZGxlciBjb2RlIGFwcGxpZXMuCjcuIFdoZW4gZGlyZWN0IENWIGV2aWRlbmNlIGV4aXN0cywgaW5jbHVkZSBhIHNob3J0IGV4YWN0IHF1b3RhdGlvbiBhbmQgaXRzIENWIHNlY3Rpb24uIFdoZW4gZGlyZWN0IGV2aWRlbmNlIGRvZXMgbm90IGV4aXN0LCByZXR1cm4gbm8gcXVvdGF0aW9uLiBOZXZlciBmYWJyaWNhdGUgb3IgcGFyYXBocmFzZSBhIHF1b3RhdGlvbi4KOC4gQSBza2lsbCBsaXN0ZWQgb25seSBpbiBhIHNraWxscyBzZWN0aW9uIGlzIG5vdCBhdXRvbWF0aWNhbGx5IHByb29mIG9mIGFwcGxpZWQgZXhwZXJpZW5jZS4gQWNhZGVtaWMsIHBlcnNvbmFsLCBpbnRlcm5zaGlwLCBmcmVlbGFuY2UsIGFuZCBwcm9mZXNzaW9uYWwgY29udGV4dHMgbXVzdCByZW1haW4gZGlzdGluZ3Vpc2hhYmxlIHdoZW4gdGhlIEpEIHdvcmRpbmcgbWFrZXMgdGhhdCBkaXN0aW5jdGlvbiByZWxldmFudC4KOS4gVXNlIGFkamFjZW50IG9yIHRyYW5zZmVyYWJsZSBldmlkZW5jZSBvbmx5IHdoZXJlIHRoZSBoYW5kbGVyIHJ1bGUgZXhwbGljaXRseSBhbGxvd3MgaXQsIGFuZCBzdGF0ZSB3aHkgaXQgaXMgYWRqYWNlbnQgcmF0aGVyIHRoYW4gZXhhY3QuCgpBUFBST1ZFRCBIQU5ETEVSIFJVTEVTCgpbSF9URUNIXSBjYXRlZ29yeSB0ZWNoX3NraWxsCi0gSF9URUNIXzAxIE5PX0VWSURFTkNFOiBObyBleGFjdCBza2lsbCwgYWNjZXB0ZWQgYWxpYXMsIG9yIHJlbGV2YW50IHRyYW5zZmVyYWJsZSBmb3VuZGF0aW9uIGlzIGZvdW5kLgotIEhfVEVDSF8wMiBJTkRJUkVDVF9NQVRDSDogVGhlIGV4YWN0IHNraWxsIGlzIGFic2VudCwgYnV0IHRoZSBDViBjb250YWlucyBhIGNsZWFyIHRyYW5zZmVyYWJsZSBvciBhZGphY2VudCBmb3VuZGF0aW9uLgotIEhfVEVDSF8wMyBNRU5USU9OX09OTFk6IFRoZSBleGFjdCBza2lsbCBvciBhY2NlcHRlZCBhbGlhcyBhcHBlYXJzLCBidXQgd2l0aG91dCBhIGNvbmNyZXRlIGFjdGlvbiBvciByZXNwb25zaWJpbGl0eS4KLSBIX1RFQ0hfMDQgQVBQTElFRF9NQVRDSDogVGhlIGV4YWN0IHNraWxsIG9yIGFjY2VwdGVkIGFsaWFzIGlzIHVzZWQgd2l0aCBhIGNsZWFyIGFjdGlvbiBpbiBhIHByb2plY3QsIGludGVybnNoaXAsIGZyZWVsYW5jZSBlbmdhZ2VtZW50LCBvciBqb2IuCi0gSF9URUNIXzA1IEZVTExfTUFUQ0g6IEV4YWN0IGFwcGxpZWQgZXZpZGVuY2UgYWxzbyBzYXRpc2ZpZXMgdGhlIHByb2ZpY2llbmN5LCBzY29wZSwgb3IgY29udGV4dCBzdGF0ZWQgYnkgdGhlIEpELgoKW0hfRVhQX0RVUkFUSU9OXSBjYXRlZ29yeSBleHBlcmllbmNlIHdoZW4gdGhlIEpEIHNwZWNpZmllcyBhIHJlcXVpcmVkIGR1cmF0aW9uCi0gSF9FWFBfRDAxIE5PX0VWSURFTkNFOiBUaGUgQ1YgaGFzIG5vIHJlbGV2YW50IHRpbWVsaW5lIHN1ZmZpY2llbnQgdG8gY2FsY3VsYXRlIHRoZSByZXF1aXJlZCBkdXJhdGlvbi4KLSBIX0VYUF9EMDIgSU5ESVJFQ1RfTUFUQ0g6IFJlbGV2YW50IGR1cmF0aW9uIGRpdmlkZWQgYnkgcmVxdWlyZWQgZHVyYXRpb24gaXMgYmVsb3cgMC41MC4KLSBIX0VYUF9EMDMgTUVOVElPTl9PTkxZOiBSZWxldmFudCBkdXJhdGlvbiBkaXZpZGVkIGJ5IHJlcXVpcmVkIGR1cmF0aW9uIGlzIGF0IGxlYXN0IDAuNTAgYW5kIGJlbG93IDAuODAuCi0gSF9FWFBfRDA0IEFQUExJRURfTUFUQ0g6IFJlbGV2YW50IGR1cmF0aW9uIGRpdmlkZWQgYnkgcmVxdWlyZWQgZHVyYXRpb24gaXMgYXQgbGVhc3QgMC44MCBhbmQgYmVsb3cgMS4wMC4KLSBIX0VYUF9EMDUgRlVMTF9NQVRDSDogUmVsZXZhbnQgZHVyYXRpb24gZGl2aWRlZCBieSByZXF1aXJlZCBkdXJhdGlvbiBpcyBhdCBsZWFzdCAxLjAwLgoKW0hfRVhQX0hBTkRTX09OXSBjYXRlZ29yeSBleHBlcmllbmNlIHdoZW4gdGhlIEpEIHJlcXVpcmVzIGhhbmRzLW9uIHdvcmssIHByaW9yIHJlc3BvbnNpYmlsaXR5LCBvciBwcm9mZXNzaW9uYWwgY29udGV4dCB3aXRob3V0IGEgbnVtZXJpYyBkdXJhdGlvbgotIEhfRVhQX0gwMSBOT19FVklERU5DRTogVGhlIENWIGhhcyBubyByZWxhdGVkIGFjdGlvbiBldmlkZW5jZS4KLSBIX0VYUF9IMDIgSU5ESVJFQ1RfTUFUQ0g6IFRoZSBDViBzaG93cyBvbmx5IGFkamFjZW50IGV4cG9zdXJlIG9yIGFjYWRlbWljL3BlcnNvbmFsLXByb2plY3Qgd29yayB3aGlsZSB0aGUgSkQgcmVxdWlyZXMgcHJvZmVzc2lvbmFsIGNvbnRleHQuCi0gSF9FWFBfSDAzIE1FTlRJT05fT05MWTogVGhlIENWIG1lbnRpb25zIGV4cG9zdXJlIG9yIHByb2Nlc3MgZmFtaWxpYXJpdHkgYnV0IGdpdmVzIG5vIGNvbmNyZXRlIHJlbGF0ZWQgcmVzcG9uc2liaWxpdHkuCi0gSF9FWFBfSDA0IEFQUExJRURfTUFUQ0g6IFRoZSBDViBzaG93cyBhIGRpcmVjdCByZXNwb25zaWJpbGl0eSBpbiBhIHByb2plY3QsIGludGVybnNoaXAsIGZyZWVsYW5jZSBlbmdhZ2VtZW50LCBvciBqb2IgdGhhdCBpcyBhY2NlcHRlZCBieSB0aGUgSkQgd29yZGluZy4KLSBIX0VYUF9IMDUgRlVMTF9NQVRDSDogVGhlIGNvbnRleHQsIHJlc3BvbnNpYmlsaXR5LCBhbmQgc2NvcGUgZnVsbHkgc2F0aXNmeSB0aGUgSkQgd29yZGluZy4KCltIX0VYUF9OT1RfQVBQTElDQUJMRV0gY2F0ZWdvcnkgZXhwZXJpZW5jZQotIEhfRVhQXzAwIE5PVF9BUFBMSUNBQkxFOiBVc2Ugb25seSB3aGVuIHRoZSBzdXBwbGllZCBpdGVtIGRvZXMgbm90IGFjdHVhbGx5IHJlcXVpcmUgZHVyYXRpb24sIGhhbmRzLW9uIGV4cGVyaWVuY2UsIHByaW9yIHJlc3BvbnNpYmlsaXR5LCBvciBwcm9mZXNzaW9uYWwgY29udGV4dC4KCltIX0VEVV0gY2F0ZWdvcnkgZWR1Y2F0aW9uCi0gSF9FRFVfMDAgTk9UX0FQUExJQ0FCTEU6IFVzZSBvbmx5IHdoZW4gdGhlIHN1cHBsaWVkIGl0ZW0gZG9lcyBub3QgYWN0dWFsbHkgcmVxdWlyZSBhIGRlZ3JlZSwgc3R1ZHkgc3RhdHVzLCBvciBtYWpvci4KLSBIX0VEVV8wMSBOT19FVklERU5DRTogVGhlIENWIGRvZXMgbm90IGNvbnRhaW4gZW5vdWdoIGVkdWNhdGlvbiBpbmZvcm1hdGlvbiB0byB2ZXJpZnkgdGhlIHJlcXVpcmVtZW50LgotIEhfRURVXzAyIE5PX01BVENIOiBDViBldmlkZW5jZSBzaG93cyB0aGUgbWFuZGF0b3J5IG1pbmltdW0gaXMgbm90IG1ldCBhbmQgdGhlIEpEIGRvZXMgbm90IGFsbG93IGFuIGVxdWl2YWxlbnQuCi0gSF9FRFVfMDMgSU5ESVJFQ1RfTUFUQ0g6IEVkdWNhdGlvbiBpcyBwcmVzZW50IGJ1dCBtYXRlcmlhbGx5IGxvd2VyLCBvciB0aGUgbWFqb3IgaXMgdW5yZWxhdGVkIHdoZXJlIHRoZSBmaWVsZCBpcyBtYW5kYXRvcnkuCi0gSF9FRFVfMDQgTUVOVElPTl9PTkxZOiBUaGUgZGVncmVlIGlzIG9uZSBsZXZlbCBsb3dlciwgb3IgdGhlIG1ham9yIGlzIGluZGlyZWN0bHkgcmVsYXRlZC4KLSBIX0VEVV8wNSBBUFBMSUVEX01BVENIOiBUaGUgbWFqb3IgbWF0Y2hlcyBidXQgdGhlIGNhbmRpZGF0ZSBpcyBzdGlsbCBjb21wbGV0aW5nIG9yIGF3YWl0aW5nIGdyYWR1YXRpb24sIG9yIGFuIGVxdWl2YWxlbnQgcGF0aCBuZWFybHkgc2F0aXNmaWVzIHRoZSB3b3JkaW5nLgotIEhfRURVXzA2IEZVTExfTUFUQ0g6IERlZ3JlZSwgY29tcGxldGlvbiBzdGF0dXMsIGFuZCBtYWpvciBtYXRjaCwgb3IgdGhlIGNhbmRpZGF0ZSBmdWxseSBzYXRpc2ZpZXMgYW4gYWxsb3dlZCBlcXVpdmFsZW50IHBhdGguCgpbSF9MQU5HX1FVQU5USUZJRURdIGNhdGVnb3J5IGxhbmd1YWdlIHdoZW4gdGhlIEpEIHNwZWNpZmllcyBhIGNlcnRpZmljYXRlIG9yIG1pbmltdW0gc2NvcmUKLSBIX0xBTkdfUTAxIE5PX0VWSURFTkNFOiBUaGUgQ1YgaGFzIG5vIGNvcnJlc3BvbmRpbmcgY2VydGlmaWNhdGUgb3Igc2NvcmUuCi0gSF9MQU5HX1EwMiBJTkRJUkVDVF9NQVRDSDogQ2VydGlmaWNhdGUgc2NvcmUgZGl2aWRlZCBieSB0aGUgcmVxdWlyZWQgbWluaW11bSBpcyBiZWxvdyAwLjUwLgotIEhfTEFOR19RMDMgTUVOVElPTl9PTkxZOiBDZXJ0aWZpY2F0ZSBzY29yZSBkaXZpZGVkIGJ5IHRoZSByZXF1aXJlZCBtaW5pbXVtIGlzIGF0IGxlYXN0IDAuNTAgYW5kIGJlbG93IDAuODAuCi0gSF9MQU5HX1EwNCBBUFBMSUVEX01BVENIOiBDZXJ0aWZpY2F0ZSBzY29yZSBkaXZpZGVkIGJ5IHRoZSByZXF1aXJlZCBtaW5pbXVtIGlzIGF0IGxlYXN0IDAuODAgYW5kIGJlbG93IDEuMDAuCi0gSF9MQU5HX1EwNSBGVUxMX01BVENIOiBDZXJ0aWZpY2F0ZSBzY29yZSBtZWV0cyBvciBleGNlZWRzIHRoZSByZXF1aXJlZCBtaW5pbXVtLgoKW0hfTEFOR19GVU5DVElPTkFMXSBjYXRlZ29yeSBsYW5ndWFnZSB3aGVuIHRoZSBKRCByZXF1aXJlcyBwcmFjdGljYWwgbGFuZ3VhZ2UgYWJpbGl0eSB3aXRob3V0IGEgbnVtZXJpYyB0aHJlc2hvbGQKLSBIX0xBTkdfRjAxIE5PX0VWSURFTkNFOiBUaGUgQ1YgaGFzIG5vIHByYWN0aWNhbCBzaWduYWwgZm9yIHRoZSByZXF1aXJlZCBsYW5ndWFnZS4KLSBIX0xBTkdfRjAyIElORElSRUNUX01BVENIOiBUaGUgQ1YgY29udGFpbnMgb25seSBpbmRpcmVjdCBldmlkZW5jZS4KLSBIX0xBTkdfRjAzIE1FTlRJT05fT05MWTogVGhlIENWIG1lbnRpb25zIGEgcHJvZmljaWVuY3kgbGV2ZWwsIGNvdXJzZSwgb3IgY2VydGlmaWNhdGUgd2l0aG91dCBhIHZlcmlmaWFibGUgdGhyZXNob2xkIG9yIGFwcGxpZWQgY29udGV4dC4KLSBIX0xBTkdfRjA0IEFQUExJRURfTUFUQ0g6IFRoZSBDViBzaG93cyBsYW5ndWFnZSB1c2UgaW4gc3R1ZHksIGEgcHJvamVjdCwgb3Igd29yaywgYnV0IHRoZSBjb250ZXh0IGRvZXMgbm90IGZ1bGx5IG1hdGNoIHRoZSBKRC4KLSBIX0xBTkdfRjA1IEZVTExfTUFUQ0g6IERpcmVjdCBldmlkZW5jZSBtYXRjaGVzIHRoZSBsYW5ndWFnZSBza2lsbCBhbmQgY29udGV4dCByZXF1aXJlZCBieSB0aGUgSkQuCgpbSF9MQU5HX05PVF9BUFBMSUNBQkxFXSBjYXRlZ29yeSBsYW5ndWFnZQotIEhfTEFOR18wMCBOT1RfQVBQTElDQUJMRTogVXNlIG9ubHkgd2hlbiB0aGUgc3VwcGxpZWQgaXRlbSBkb2VzIG5vdCBhY3R1YWxseSBjb250YWluIGEgbGFuZ3VhZ2UgcmVxdWlyZW1lbnQuCgpbSF9ET01BSU5dIGNhdGVnb3J5IGRvbWFpbl9rbm93bGVkZ2UKLSBIX0RPTUFJTl8wMSBOT19FVklERU5DRTogTm8gc2lnbmFsIGV4aXN0cyBmb3IgdGhlIHJlcXVpcmVkIGRvbWFpbi4KLSBIX0RPTUFJTl8wMiBJTkRJUkVDVF9NQVRDSDogVGhlIENWIHNob3dzIGFkamFjZW50IGtub3dsZWRnZSBvciBpbmRpcmVjdGx5IHJlbGF0ZWQgY29uY2VwdHMuCi0gSF9ET01BSU5fMDMgTUVOVElPTl9PTkxZOiBUaGUgZG9tYWluIG5hbWUgYXBwZWFycyBvciBhIHNtYWxsIHByb2plY3QgZXhpc3RzLCBidXQgbm8gY29uY3JldGUgZG9tYWluIHJlc3BvbnNpYmlsaXR5IGlzIHNob3duLgotIEhfRE9NQUlOXzA0IEFQUExJRURfTUFUQ0g6IFRoZSBDViBzaG93cyB3b3JrIG9uIGEgZG9tYWluIHByb2JsZW0gaW4gYSBwcm9qZWN0LCBpbnRlcm5zaGlwLCBmcmVlbGFuY2UgZW5nYWdlbWVudCwgb3Igam9iLgotIEhfRE9NQUlOXzA1IEZVTExfTUFUQ0g6IERpcmVjdCBldmlkZW5jZSBzYXRpc2ZpZXMgdGhlIGRvbWFpbiBkZXB0aCBhbmQgY29udGV4dCBzdGF0ZWQgYnkgdGhlIEpELgoKW0hfU09GVF0gY2F0ZWdvcnkgc29mdF9za2lsbAotIEhfU09GVF8wMSBOT19FVklERU5DRTogTm8gcmVsZXZhbnQgYmVoYXZpb3JhbCBldmlkZW5jZSBpcyBwcmVzZW50LgotIEhfU09GVF8wMiBJTkRJUkVDVF9NQVRDSDogT25seSBhIGdlbmVyaWMga2V5d29yZCBvciB2ZXJ5IHdlYWsgc2lnbmFsIGlzIHByZXNlbnQuCi0gSF9TT0ZUXzAzIE1FTlRJT05fT05MWTogT25lIGNvbnRleHR1YWwgc2lnbmFsIGV4aXN0cywgYnV0IHRoZSBiZWhhdmlvciBvciBjb250cmlidXRpb24gaXMgdW5jbGVhci4KLSBIX1NPRlRfMDQgQVBQTElFRF9NQVRDSDogT25lIGRpcmVjdCBiZWhhdmlvcmFsIGV4YW1wbGUgc3VwcG9ydHMgdGhlIHJlcXVpcmVtZW50LgotIEhfU09GVF8wNSBGVUxMX01BVENIOiBNdWx0aXBsZSBpbmRlcGVuZGVudCBleGFtcGxlcywgb3Igb25lIGVzcGVjaWFsbHkgc3Ryb25nIGNvbnRleHR1YWwgZXhhbXBsZSwgc3VwcG9ydCB0aGUgcmVxdWlyZW1lbnQuCgpTT0ZULVNLSUxMIEVWSURFTkNFIEdVSURBTkNFCi0gU2VsZi1sZWFybmluZzogY2VydGlmaWNhdGlvbnMgb3Igc2lkZSBwcm9qZWN0cyBhcmUgc3Ryb25nZXIgdGhhbiBhIGJyb2FkIHN0YWNrIHdpdGhvdXQgbGVhcm5pbmcgY29udGV4dC4KLSBUZWFtd29yazogYSBzdGF0ZWQgcm9sZSBhbmQgY29udHJpYnV0aW9uIGluIGEgdGVhbSBhcmUgc3Ryb25nZXIgdGhhbiB0aGUgd29yZCAidGVhbSIgYWxvbmUuCi0gQ29tbXVuaWNhdGlvbjogYSBjb25jcmV0ZSBzdGFrZWhvbGRlciwgcHJlc2VudGF0aW9uLCBkb2N1bWVudGF0aW9uLCByZXZpZXcsIG9yIGNvb3JkaW5hdGlvbiBhY3Rpb24gaXMgc3Ryb25nZXIgdGhhbiBhIGdlbmVyaWMgc2VsZi1jbGFpbS4KLSBQcm9ibGVtLXNvbHZpbmc6IGEgY29uY3JldGUgdGVjaG5pY2FsIG9yIHByb2R1Y3QgY2hhbGxlbmdlIGFuZCB0aGUgY2FuZGlkYXRlJ3MgYWN0aW9uIGFyZSBzdHJvbmdlciB0aGFuIGdlbmVyaWMgQ1JVRCB3b3JrLgoKRklOQUwgQ0hFQ0sKLSBQcm9kdWNlIG9uZSBldmFsdWF0aW9uIGZvciBldmVyeSBzdXBwbGllZCBpdGVtIGFuZCBubyBldmFsdWF0aW9uIGZvciBhbnl0aGluZyBlbHNlLgotIFByZXNlcnZlIGV2ZXJ5IHJlcUlkIGV4YWN0bHkuCi0gVXNlIG9ubHkgdGhlIGFwcHJvdmVkIGhhbmRsZXIgY29kZXMgZm9yIHRoZSBzdXBwbGllZCBjYXRlZ29yeS4KLSBLZWVwIHJlYXNvbmluZyB1c2VyLWZhY2luZywgc3BlY2lmaWMsIGFuZCBncm91bmRlZCBpbiB0aGUgc3VwcGxpZWQgZGF0YS4KLSBEbyBub3QgY2FsY3VsYXRlIHRvdGFscywgcmVzdWx0IGJhbmRzLCBhcHBsaWNhdGlvbi1vd25lZCBzY29yZSBhZGp1c3RtZW50cywgY3JpdGljYWwgZ2FwcywgaW1wcm92ZW1lbnQgc3VnZ2VzdGlvbnMsIG9yIGdyb3VwIHNjb3Jlcy4gVGhlIGFwcGxpY2F0aW9uIG93bnMgdGhvc2Ugb3BlcmF0aW9ucy4K";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var matchingContent = Decode(MatchingContentBase64);
            migrationBuilder.Sql(
                """
                DO $seed$
                DECLARE
                    matching_prompt_id uuid;
                    active_matching_id uuid;
                    cv_system_active_id uuid;
                    cv_user_active_id uuid;
                    jd_system_active_id uuid;
                    jd_user_active_id uuid;
                    matching_content text := $jd_matching_v3$
                """ + matchingContent + """
                $jd_matching_v3$;
                BEGIN
                    LOCK TABLE "PromptVersions" IN SHARE ROW EXCLUSIVE MODE;

                    SELECT "Id" INTO STRICT matching_prompt_id
                    FROM "Prompts"
                    WHERE "PromptKey" = 'JD_MATCHING_PROMPT'
                    FOR UPDATE;

                    SELECT v."Id" INTO STRICT cv_system_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'CV_ANALYSIS_SYSTEM' AND v."IsActive";
                    SELECT v."Id" INTO STRICT cv_user_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'CV_ANALYSIS_USER' AND v."IsActive";
                    SELECT v."Id" INTO STRICT jd_system_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'JD_ANALYSIS_V2_SYSTEM' AND v."IsActive";
                    SELECT v."Id" INTO STRICT jd_user_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'JD_ANALYSIS_V2_USER' AND v."IsActive";

                    SELECT "Id" INTO STRICT active_matching_id
                    FROM "PromptVersions"
                    WHERE "PromptId" = matching_prompt_id AND "IsActive"
                    FOR UPDATE;

                    IF EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.0'
                          AND "Id" <> '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_DUPLICATE_TAG';
                    END IF;

                    IF active_matching_id = '4969f6f7-5696-4700-8817-1fee806ecf9e'::uuid THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = active_matching_id
                              AND "PromptId" = matching_prompt_id
                              AND "VersionTag" = 'v2.0.1'
                              AND "ModelConfig" IS NULL
                              AND md5("Content") = '345bd6a7ab04563b6c42bc9d3c2071c9'
                        ) THEN
                            RAISE EXCEPTION 'JD_MATCHING_V3_EXPECTED_V2_MISMATCH';
                        END IF;
                    ELSIF active_matching_id = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = active_matching_id
                              AND "PromptId" = matching_prompt_id
                              AND "VersionTag" = 'v3.0.0'
                              AND "Content" = matching_content
                              AND "ModelConfig" IS NULL
                        ) THEN
                            RAISE EXCEPTION 'JD_MATCHING_V3_REPLAY_MISMATCH';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'JD_MATCHING_V3_UNEXPECTED_ACTIVE_VERSION';
                    END IF;

                    INSERT INTO "PromptVersions"
                        ("Id", "PromptId", "VersionTag", "Content", "ModelConfig", "IsActive", "CreatedBy", "CreatedAt")
                    VALUES
                        ('52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid, matching_prompt_id, 'v3.0.0', matching_content, NULL, FALSE,
                         '00000000-0000-0000-0000-000000000000'::uuid, CURRENT_TIMESTAMP)
                    ON CONFLICT ("Id") DO NOTHING;

                    IF NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                          AND "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.0'
                          AND "Content" = matching_content
                          AND md5("Content") = '694eec28e412f4822156b874cd5a4f80'
                          AND "ModelConfig" IS NULL
                          AND "CreatedBy" = '00000000-0000-0000-0000-000000000000'::uuid
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_FIXED_ROW_MISMATCH';
                    END IF;

                    UPDATE "PromptVersions" SET "IsActive" = FALSE
                    WHERE "PromptId" = matching_prompt_id AND "IsActive";
                    UPDATE "PromptVersions" SET "IsActive" = TRUE
                    WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                      AND "PromptId" = matching_prompt_id;
                    UPDATE "Prompts" SET "UpdatedAt" = CURRENT_TIMESTAMP
                    WHERE "Id" = matching_prompt_id;

                    IF (SELECT COUNT(*) FROM "PromptVersions" WHERE "PromptId" = matching_prompt_id AND "IsActive") <> 1
                    OR NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                          AND "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.0'
                          AND "Content" = matching_content
                          AND md5("Content") = '694eec28e412f4822156b874cd5a4f80'
                          AND "ModelConfig" IS NULL
                          AND "IsActive"
                    )
                    OR (length(matching_content) - length(replace(matching_content, '[CV_TEXT]', ''))) / length('[CV_TEXT]') <> 1
                    OR (length(matching_content) - length(replace(matching_content, '[PARSED_JD_REQUIREMENTS]', ''))) / length('[PARSED_JD_REQUIREMENTS]') <> 1
                    OR position('--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---' IN matching_content) > 0
                    OR position('SCHEMA OUTPUT BẮT BUỘC' IN matching_content) > 0
                    OR position('"schemaVersion"' IN matching_content) > 0
                    THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_POSTCONDITION_FAILED';
                    END IF;

                    IF cv_system_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'CV_ANALYSIS_SYSTEM' AND v."IsActive")
                    OR cv_user_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'CV_ANALYSIS_USER' AND v."IsActive")
                    OR jd_system_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'JD_ANALYSIS_V2_SYSTEM' AND v."IsActive")
                    OR jd_user_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'JD_ANALYSIS_V2_USER' AND v."IsActive")
                    THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_PARSER_PAIR_CHANGED';
                    END IF;
                END
                $seed$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var matchingContent = Decode(MatchingContentBase64);
            migrationBuilder.Sql(
                """
                DO $seed_down$
                DECLARE
                    matching_prompt_id uuid;
                    active_matching_id uuid;
                    cv_system_active_id uuid;
                    cv_user_active_id uuid;
                    jd_system_active_id uuid;
                    jd_user_active_id uuid;
                    matching_content text := $jd_matching_v3$
                """ + matchingContent + """
                $jd_matching_v3$;
                BEGIN
                    LOCK TABLE "PromptVersions" IN SHARE ROW EXCLUSIVE MODE;

                    SELECT "Id" INTO STRICT matching_prompt_id
                    FROM "Prompts"
                    WHERE "PromptKey" = 'JD_MATCHING_PROMPT'
                    FOR UPDATE;

                    SELECT v."Id" INTO STRICT cv_system_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'CV_ANALYSIS_SYSTEM' AND v."IsActive";
                    SELECT v."Id" INTO STRICT cv_user_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'CV_ANALYSIS_USER' AND v."IsActive";
                    SELECT v."Id" INTO STRICT jd_system_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'JD_ANALYSIS_V2_SYSTEM' AND v."IsActive";
                    SELECT v."Id" INTO STRICT jd_user_active_id
                    FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                    WHERE p."PromptKey" = 'JD_ANALYSIS_V2_USER' AND v."IsActive";

                    IF NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                          AND "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.0'
                          AND "Content" = matching_content
                          AND md5("Content") = '694eec28e412f4822156b874cd5a4f80'
                          AND "ModelConfig" IS NULL
                          AND "CreatedBy" = '00000000-0000-0000-0000-000000000000'::uuid
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_DOWN_FIXED_ROW_MISMATCH';
                    END IF;

                    SELECT "Id" INTO STRICT active_matching_id
                    FROM "PromptVersions"
                    WHERE "PromptId" = matching_prompt_id AND "IsActive"
                    FOR UPDATE;

                    IF active_matching_id = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = '4969f6f7-5696-4700-8817-1fee806ecf9e'::uuid
                              AND "PromptId" = matching_prompt_id
                              AND "VersionTag" = 'v2.0.1'
                              AND "ModelConfig" IS NULL
                              AND md5("Content") = '345bd6a7ab04563b6c42bc9d3c2071c9'
                        ) THEN
                            RAISE EXCEPTION 'JD_MATCHING_V3_DOWN_V2_FALLBACK_MISMATCH';
                        END IF;

                        UPDATE "PromptVersions" SET "IsActive" = FALSE
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid;
                        UPDATE "PromptVersions" SET "IsActive" = TRUE
                        WHERE "Id" = '4969f6f7-5696-4700-8817-1fee806ecf9e'::uuid
                          AND "PromptId" = matching_prompt_id;
                    ELSIF active_matching_id = '4969f6f7-5696-4700-8817-1fee806ecf9e'::uuid THEN
                        IF EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid AND "IsActive"
                        )
                        OR NOT EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = active_matching_id
                              AND "PromptId" = matching_prompt_id
                              AND "VersionTag" = 'v2.0.1'
                              AND "ModelConfig" IS NULL
                              AND md5("Content") = '345bd6a7ab04563b6c42bc9d3c2071c9'
                        ) THEN
                            RAISE EXCEPTION 'JD_MATCHING_V3_DOWN_REPLAY_MISMATCH';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'JD_MATCHING_V3_DOWN_NEWER_ACTIVE_VERSION';
                    END IF;

                    UPDATE "Prompts" SET "UpdatedAt" = CURRENT_TIMESTAMP
                    WHERE "Id" = matching_prompt_id;

                    IF (SELECT COUNT(*) FROM "PromptVersions" WHERE "PromptId" = matching_prompt_id AND "IsActive") <> 1
                    OR NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '4969f6f7-5696-4700-8817-1fee806ecf9e'::uuid
                          AND "PromptId" = matching_prompt_id AND "IsActive"
                    )
                    OR EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid AND "IsActive"
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_DOWN_POSTCONDITION_FAILED';
                    END IF;

                    IF cv_system_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'CV_ANALYSIS_SYSTEM' AND v."IsActive")
                    OR cv_user_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'CV_ANALYSIS_USER' AND v."IsActive")
                    OR jd_system_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'JD_ANALYSIS_V2_SYSTEM' AND v."IsActive")
                    OR jd_user_active_id IS DISTINCT FROM (
                        SELECT v."Id" FROM "PromptVersions" v JOIN "Prompts" p ON p."Id" = v."PromptId"
                        WHERE p."PromptKey" = 'JD_ANALYSIS_V2_USER' AND v."IsActive")
                    THEN
                        RAISE EXCEPTION 'JD_MATCHING_V3_DOWN_PARSER_PAIR_CHANGED';
                    END IF;
                END
                $seed_down$;
                """);
        }

        private static string Decode(string base64) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
