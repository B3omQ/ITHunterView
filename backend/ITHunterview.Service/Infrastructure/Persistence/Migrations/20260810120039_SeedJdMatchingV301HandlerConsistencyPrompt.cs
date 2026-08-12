using Microsoft.EntityFrameworkCore.Migrations;
using System.Text;

#nullable disable

namespace ITHunterview.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SeedJdMatchingV301HandlerConsistencyPrompt : Migration
    {
        private const string MatchingContentBase64 = "WW91IGFyZSBhIHByb2Zlc3Npb25hbCBJVCByZWNydWl0bWVudCBhc3Npc3RhbnQuIFlvdXIgdGFzayBpcyB0byBldmFsdWF0ZSBob3cgd2VsbCB0aGUgY2FuZGlkYXRlIENWIHN1cHBvcnRzIGVhY2ggc3VwcGxpZWQgSkQgcmVxdWlyZW1lbnQgaXRlbS4KCkV2ZXJ5IHRleHR1YWwgZmllbGQgaW4gdGhlIHJlc3BvbnNlIG11c3QgYmUgd3JpdHRlbiBpbiBFbmdsaXNoLiBVc2Ugb25seSB0aGUgc3VwcGxpZWQgQ1YgYW5kIEpEIHJlcXVpcmVtZW50IGRhdGEuIE5ldmVyIGludmVudCBhIHNraWxsLCBkdXJhdGlvbiwgcXVhbGlmaWNhdGlvbiwgcXVvdGF0aW9uLCBzZWN0aW9uLCByZXNwb25zaWJpbGl0eSwgcHJvamVjdCwgb3V0Y29tZSwgb3IgZW1wbG95bWVudCBjb250ZXh0LgoKSU5QVVQgREFUQQotLS0gU1RBUlQgQ1YgLS0tCltDVl9URVhUXQotLS0gRU5EIENWIC0tLQoKLS0tIFNUQVJUIEpEIFJFUVVJUkVNRU5UUyAtLS0KW1BBUlNFRF9KRF9SRVFVSVJFTUVOVFNdCi0tLSBFTkQgSkQgUkVRVUlSRU1FTlRTIC0tLQoKVEFTSwoxLiBFdmFsdWF0ZSBldmVyeSBzdXBwbGllZCByZXF1aXJlbWVudCBpdGVtIGluZGVwZW5kZW50bHksIGluY2x1ZGluZyBldmVyeSBhbHRlcm5hdGl2ZSBpbnNpZGUgb25lX29mIGFuZCBhdF9sZWFzdF9uIGdyb3Vwcy4KMi4gUmV0dXJuIHRoZSBleGFjdCByZXFJZCBzdXBwbGllZCBmb3IgdGhhdCBpdGVtLiBOZXZlciBjcmVhdGUsIHJld3JpdGUsIG1lcmdlLCBvbWl0LCBvciByZXVzZSBhbiBJRC4KMy4gU2VsZWN0IGV4YWN0bHkgb25lIGFwcHJvdmVkIGhhbmRsZXJDb2RlIGZvciB0aGUgaXRlbSdzIHN1cHBsaWVkIGNhdGVnb3J5LiBEbyBub3QgcmV0dXJuIGEgbnVtZXJpYyBzY29yZTsgdGhlIGFwcGxpY2F0aW9uIG1hcHMgaGFuZGxlckNvZGUgdG8gaXRzIGZpeGVkIHNjb3JlLgpUaGUgc3VwcGxpZWQgcmVxdWlyZW1lbnRzIGFyZSBhbHJlYWR5IGFwcGxpY2FibGUgSkQgaXRlbXMuIE5ldmVyIHJldHVybgpOT1RfQVBQTElDQUJMRSBvciBFWENMVURFRCBoYW5kbGVyIGNvZGVzLiBJZiB0aGUgQ1YgaGFzIG5vIHN1cHBvcnRpbmcgZXZpZGVuY2UsCnJldHVybiB0aGUgYXBwcm9wcmlhdGUgc2NvcmUtYmVhcmluZyBOT19FVklERU5DRSBoYW5kbGVyIGZvciB0aGUgaXRlbSBpbnN0ZWFkLgo0LiBSZXNwZWN0IHRoZSBjb21wbGV0ZSBtZWFuaW5nIG9mIHRoZSBzdXBwbGllZCByZXF1aXJlbWVudCwgaW5jbHVkaW5nIG1pbmltdW0geWVhcnMsIHByb2Zlc3Npb25hbCBvciBoYW5kcy1vbiBjb250ZXh0LCBhbHRlcm5hdGl2ZXMsIGRlZ3JlZSBzdGF0dXMsIGNlcnRpZmljYXRpb24gdGhyZXNob2xkcywgbGFuZ3VhZ2UgY29udGV4dCwgYW5kIGRvbWFpbiBkZXB0aC4KNS4gRG8gbm90IGRlY2lkZSBncm91cCBhZ2dyZWdhdGlvbi4gVGhlIGFwcGxpY2F0aW9uIGFwcGxpZXMgYWxsX29mLCBvbmVfb2YsIGFuZCBhdF9sZWFzdF9uIGFmdGVyIGV2ZXJ5IGl0ZW0gaGFzIGJlZW4gZXZhbHVhdGVkLgo2LiBGb3IgZWFjaCBpdGVtLCBleHBsYWluIGluIGRldGFpbDogd2hhdCB0aGUgSkQgcmVxdWlyZXMsIHdoYXQgcmVsZXZhbnQgQ1YgZXZpZGVuY2UgZXhpc3RzIG9yIGlzIGFic2VudCwgYW5kIHdoeSB0aGUgc2VsZWN0ZWQgaGFuZGxlciBjb2RlIGFwcGxpZXMuCjcuIFdoZW4gZGlyZWN0IENWIGV2aWRlbmNlIGV4aXN0cywgaW5jbHVkZSBhIHNob3J0IGV4YWN0IHF1b3RhdGlvbiBhbmQgaXRzIENWIHNlY3Rpb24uIFdoZW4gZGlyZWN0IGV2aWRlbmNlIGRvZXMgbm90IGV4aXN0LCByZXR1cm4gbm8gcXVvdGF0aW9uLiBOZXZlciBmYWJyaWNhdGUgb3IgcGFyYXBocmFzZSBhIHF1b3RhdGlvbi4KOC4gQSBza2lsbCBsaXN0ZWQgb25seSBpbiBhIHNraWxscyBzZWN0aW9uIGlzIG5vdCBhdXRvbWF0aWNhbGx5IHByb29mIG9mIGFwcGxpZWQgZXhwZXJpZW5jZS4gQWNhZGVtaWMsIHBlcnNvbmFsLCBpbnRlcm5zaGlwLCBmcmVlbGFuY2UsIGFuZCBwcm9mZXNzaW9uYWwgY29udGV4dHMgbXVzdCByZW1haW4gZGlzdGluZ3Vpc2hhYmxlIHdoZW4gdGhlIEpEIHdvcmRpbmcgbWFrZXMgdGhhdCBkaXN0aW5jdGlvbiByZWxldmFudC4KOS4gVXNlIGFkamFjZW50IG9yIHRyYW5zZmVyYWJsZSBldmlkZW5jZSBvbmx5IHdoZXJlIHRoZSBoYW5kbGVyIHJ1bGUgZXhwbGljaXRseSBhbGxvd3MgaXQsIGFuZCBzdGF0ZSB3aHkgaXQgaXMgYWRqYWNlbnQgcmF0aGVyIHRoYW4gZXhhY3QuCgpBUFBST1ZFRCBIQU5ETEVSIFJVTEVTCgpbSF9URUNIXSBjYXRlZ29yeSB0ZWNoX3NraWxsCi0gSF9URUNIXzAxIE5PX0VWSURFTkNFOiBObyBleGFjdCBza2lsbCwgYWNjZXB0ZWQgYWxpYXMsIG9yIHJlbGV2YW50IHRyYW5zZmVyYWJsZSBmb3VuZGF0aW9uIGlzIGZvdW5kLgotIEhfVEVDSF8wMiBJTkRJUkVDVF9NQVRDSDogVGhlIGV4YWN0IHNraWxsIGlzIGFic2VudCwgYnV0IHRoZSBDViBjb250YWlucyBhIGNsZWFyIHRyYW5zZmVyYWJsZSBvciBhZGphY2VudCBmb3VuZGF0aW9uLgotIEhfVEVDSF8wMyBNRU5USU9OX09OTFk6IFRoZSBleGFjdCBza2lsbCBvciBhY2NlcHRlZCBhbGlhcyBhcHBlYXJzLCBidXQgd2l0aG91dCBhIGNvbmNyZXRlIGFjdGlvbiBvciByZXNwb25zaWJpbGl0eS4KLSBIX1RFQ0hfMDQgQVBQTElFRF9NQVRDSDogVGhlIGV4YWN0IHNraWxsIG9yIGFjY2VwdGVkIGFsaWFzIGlzIHVzZWQgd2l0aCBhIGNsZWFyIGFjdGlvbiBpbiBhIHByb2plY3QsIGludGVybnNoaXAsIGZyZWVsYW5jZSBlbmdhZ2VtZW50LCBvciBqb2IuCi0gSF9URUNIXzA1IEZVTExfTUFUQ0g6IEV4YWN0IGFwcGxpZWQgZXZpZGVuY2UgYWxzbyBzYXRpc2ZpZXMgdGhlIHByb2ZpY2llbmN5LCBzY29wZSwgb3IgY29udGV4dCBzdGF0ZWQgYnkgdGhlIEpELgoKW0hfRVhQX0RVUkFUSU9OXSBjYXRlZ29yeSBleHBlcmllbmNlIHdoZW4gdGhlIEpEIHNwZWNpZmllcyBhIHJlcXVpcmVkIGR1cmF0aW9uCi0gSF9FWFBfRDAxIE5PX0VWSURFTkNFOiBUaGUgQ1YgaGFzIG5vIHJlbGV2YW50IHRpbWVsaW5lIHN1ZmZpY2llbnQgdG8gY2FsY3VsYXRlIHRoZSByZXF1aXJlZCBkdXJhdGlvbi4KLSBIX0VYUF9EMDIgSU5ESVJFQ1RfTUFUQ0g6IFJlbGV2YW50IGR1cmF0aW9uIGRpdmlkZWQgYnkgcmVxdWlyZWQgZHVyYXRpb24gaXMgYmVsb3cgMC41MC4KLSBIX0VYUF9EMDMgTUVOVElPTl9PTkxZOiBSZWxldmFudCBkdXJhdGlvbiBkaXZpZGVkIGJ5IHJlcXVpcmVkIGR1cmF0aW9uIGlzIGF0IGxlYXN0IDAuNTAgYW5kIGJlbG93IDAuODAuCi0gSF9FWFBfRDA0IEFQUExJRURfTUFUQ0g6IFJlbGV2YW50IGR1cmF0aW9uIGRpdmlkZWQgYnkgcmVxdWlyZWQgZHVyYXRpb24gaXMgYXQgbGVhc3QgMC44MCBhbmQgYmVsb3cgMS4wMC4KLSBIX0VYUF9EMDUgRlVMTF9NQVRDSDogUmVsZXZhbnQgZHVyYXRpb24gZGl2aWRlZCBieSByZXF1aXJlZCBkdXJhdGlvbiBpcyBhdCBsZWFzdCAxLjAwLgoKW0hfRVhQX0hBTkRTX09OXSBjYXRlZ29yeSBleHBlcmllbmNlIHdoZW4gdGhlIEpEIHJlcXVpcmVzIGhhbmRzLW9uIHdvcmssIHByaW9yIHJlc3BvbnNpYmlsaXR5LCBvciBwcm9mZXNzaW9uYWwgY29udGV4dCB3aXRob3V0IGEgbnVtZXJpYyBkdXJhdGlvbgotIEhfRVhQX0gwMSBOT19FVklERU5DRTogVGhlIENWIGhhcyBubyByZWxhdGVkIGFjdGlvbiBldmlkZW5jZS4KLSBIX0VYUF9IMDIgSU5ESVJFQ1RfTUFUQ0g6IFRoZSBDViBzaG93cyBvbmx5IGFkamFjZW50IGV4cG9zdXJlIG9yIGFjYWRlbWljL3BlcnNvbmFsLXByb2plY3Qgd29yayB3aGlsZSB0aGUgSkQgcmVxdWlyZXMgcHJvZmVzc2lvbmFsIGNvbnRleHQuCi0gSF9FWFBfSDAzIE1FTlRJT05fT05MWTogVGhlIENWIG1lbnRpb25zIGV4cG9zdXJlIG9yIHByb2Nlc3MgZmFtaWxpYXJpdHkgYnV0IGdpdmVzIG5vIGNvbmNyZXRlIHJlbGF0ZWQgcmVzcG9uc2liaWxpdHkuCi0gSF9FWFBfSDA0IEFQUExJRURfTUFUQ0g6IFRoZSBDViBzaG93cyBhIGRpcmVjdCByZXNwb25zaWJpbGl0eSBpbiBhIHByb2plY3QsIGludGVybnNoaXAsIGZyZWVsYW5jZSBlbmdhZ2VtZW50LCBvciBqb2IgdGhhdCBpcyBhY2NlcHRlZCBieSB0aGUgSkQgd29yZGluZy4KLSBIX0VYUF9IMDUgRlVMTF9NQVRDSDogVGhlIGNvbnRleHQsIHJlc3BvbnNpYmlsaXR5LCBhbmQgc2NvcGUgZnVsbHkgc2F0aXNmeSB0aGUgSkQgd29yZGluZy4KCltIX0VEVV0gY2F0ZWdvcnkgZWR1Y2F0aW9uCi0gSF9FRFVfMDEgTk9fRVZJREVOQ0U6IFRoZSBDViBkb2VzIG5vdCBjb250YWluIGVub3VnaCBlZHVjYXRpb24gaW5mb3JtYXRpb24gdG8gdmVyaWZ5IHRoZSByZXF1aXJlbWVudC4KLSBIX0VEVV8wMiBOT19NQVRDSDogQ1YgZXZpZGVuY2Ugc2hvd3MgdGhlIG1hbmRhdG9yeSBtaW5pbXVtIGlzIG5vdCBtZXQgYW5kIHRoZSBKRCBkb2VzIG5vdCBhbGxvdyBhbiBlcXVpdmFsZW50LgotIEhfRURVXzAzIElORElSRUNUX01BVENIOiBFZHVjYXRpb24gaXMgcHJlc2VudCBidXQgbWF0ZXJpYWxseSBsb3dlciwgb3IgdGhlIG1ham9yIGlzIHVucmVsYXRlZCB3aGVyZSB0aGUgZmllbGQgaXMgbWFuZGF0b3J5LgotIEhfRURVXzA0IE1FTlRJT05fT05MWTogVGhlIGRlZ3JlZSBpcyBvbmUgbGV2ZWwgbG93ZXIsIG9yIHRoZSBtYWpvciBpcyBpbmRpcmVjdGx5IHJlbGF0ZWQuCi0gSF9FRFVfMDUgQVBQTElFRF9NQVRDSDogVGhlIG1ham9yIG1hdGNoZXMgYnV0IHRoZSBjYW5kaWRhdGUgaXMgc3RpbGwgY29tcGxldGluZyBvciBhd2FpdGluZyBncmFkdWF0aW9uLCBvciBhbiBlcXVpdmFsZW50IHBhdGggbmVhcmx5IHNhdGlzZmllcyB0aGUgd29yZGluZy4KLSBIX0VEVV8wNiBGVUxMX01BVENIOiBEZWdyZWUsIGNvbXBsZXRpb24gc3RhdHVzLCBhbmQgbWFqb3IgbWF0Y2gsIG9yIHRoZSBjYW5kaWRhdGUgZnVsbHkgc2F0aXNmaWVzIGFuIGFsbG93ZWQgZXF1aXZhbGVudCBwYXRoLgoKW0hfTEFOR19RVUFOVElGSUVEXSBjYXRlZ29yeSBsYW5ndWFnZSB3aGVuIHRoZSBKRCBzcGVjaWZpZXMgYSBjZXJ0aWZpY2F0ZSBvciBtaW5pbXVtIHNjb3JlCi0gSF9MQU5HX1EwMSBOT19FVklERU5DRTogVGhlIENWIGhhcyBubyBjb3JyZXNwb25kaW5nIGNlcnRpZmljYXRlIG9yIHNjb3JlLgotIEhfTEFOR19RMDIgSU5ESVJFQ1RfTUFUQ0g6IENlcnRpZmljYXRlIHNjb3JlIGRpdmlkZWQgYnkgdGhlIHJlcXVpcmVkIG1pbmltdW0gaXMgYmVsb3cgMC41MC4KLSBIX0xBTkdfUTAzIE1FTlRJT05fT05MWTogQ2VydGlmaWNhdGUgc2NvcmUgZGl2aWRlZCBieSB0aGUgcmVxdWlyZWQgbWluaW11bSBpcyBhdCBsZWFzdCAwLjUwIGFuZCBiZWxvdyAwLjgwLgotIEhfTEFOR19RMDQgQVBQTElFRF9NQVRDSDogQ2VydGlmaWNhdGUgc2NvcmUgZGl2aWRlZCBieSB0aGUgcmVxdWlyZWQgbWluaW11bSBpcyBhdCBsZWFzdCAwLjgwIGFuZCBiZWxvdyAxLjAwLgotIEhfTEFOR19RMDUgRlVMTF9NQVRDSDogQ2VydGlmaWNhdGUgc2NvcmUgbWVldHMgb3IgZXhjZWVkcyB0aGUgcmVxdWlyZWQgbWluaW11bS4KCltIX0xBTkdfRlVOQ1RJT05BTF0gY2F0ZWdvcnkgbGFuZ3VhZ2Ugd2hlbiB0aGUgSkQgcmVxdWlyZXMgcHJhY3RpY2FsIGxhbmd1YWdlIGFiaWxpdHkgd2l0aG91dCBhIG51bWVyaWMgdGhyZXNob2xkCi0gSF9MQU5HX0YwMSBOT19FVklERU5DRTogVGhlIENWIGhhcyBubyBwcmFjdGljYWwgc2lnbmFsIGZvciB0aGUgcmVxdWlyZWQgbGFuZ3VhZ2UuCi0gSF9MQU5HX0YwMiBJTkRJUkVDVF9NQVRDSDogVGhlIENWIGNvbnRhaW5zIG9ubHkgaW5kaXJlY3QgZXZpZGVuY2UuCi0gSF9MQU5HX0YwMyBNRU5USU9OX09OTFk6IFRoZSBDViBtZW50aW9ucyBhIHByb2ZpY2llbmN5IGxldmVsLCBjb3Vyc2UsIG9yIGNlcnRpZmljYXRlIHdpdGhvdXQgYSB2ZXJpZmlhYmxlIHRocmVzaG9sZCBvciBhcHBsaWVkIGNvbnRleHQuCi0gSF9MQU5HX0YwNCBBUFBMSUVEX01BVENIOiBUaGUgQ1Ygc2hvd3MgbGFuZ3VhZ2UgdXNlIGluIHN0dWR5LCBhIHByb2plY3QsIG9yIHdvcmssIGJ1dCB0aGUgY29udGV4dCBkb2VzIG5vdCBmdWxseSBtYXRjaCB0aGUgSkQuCi0gSF9MQU5HX0YwNSBGVUxMX01BVENIOiBEaXJlY3QgZXZpZGVuY2UgbWF0Y2hlcyB0aGUgbGFuZ3VhZ2Ugc2tpbGwgYW5kIGNvbnRleHQgcmVxdWlyZWQgYnkgdGhlIEpELgoKW0hfRE9NQUlOXSBjYXRlZ29yeSBkb21haW5fa25vd2xlZGdlCi0gSF9ET01BSU5fMDEgTk9fRVZJREVOQ0U6IE5vIHNpZ25hbCBleGlzdHMgZm9yIHRoZSByZXF1aXJlZCBkb21haW4uCi0gSF9ET01BSU5fMDIgSU5ESVJFQ1RfTUFUQ0g6IFRoZSBDViBzaG93cyBhZGphY2VudCBrbm93bGVkZ2Ugb3IgaW5kaXJlY3RseSByZWxhdGVkIGNvbmNlcHRzLgotIEhfRE9NQUlOXzAzIE1FTlRJT05fT05MWTogVGhlIGRvbWFpbiBuYW1lIGFwcGVhcnMgb3IgYSBzbWFsbCBwcm9qZWN0IGV4aXN0cywgYnV0IG5vIGNvbmNyZXRlIGRvbWFpbiByZXNwb25zaWJpbGl0eSBpcyBzaG93bi4KLSBIX0RPTUFJTl8wNCBBUFBMSUVEX01BVENIOiBUaGUgQ1Ygc2hvd3Mgd29yayBvbiBhIGRvbWFpbiBwcm9ibGVtIGluIGEgcHJvamVjdCwgaW50ZXJuc2hpcCwgZnJlZWxhbmNlIGVuZ2FnZW1lbnQsIG9yIGpvYi4KLSBIX0RPTUFJTl8wNSBGVUxMX01BVENIOiBEaXJlY3QgZXZpZGVuY2Ugc2F0aXNmaWVzIHRoZSBkb21haW4gZGVwdGggYW5kIGNvbnRleHQgc3RhdGVkIGJ5IHRoZSBKRC4KCltIX1NPRlRdIGNhdGVnb3J5IHNvZnRfc2tpbGwKLSBIX1NPRlRfMDEgTk9fRVZJREVOQ0U6IE5vIHJlbGV2YW50IGJlaGF2aW9yYWwgZXZpZGVuY2UgaXMgcHJlc2VudC4KLSBIX1NPRlRfMDIgSU5ESVJFQ1RfTUFUQ0g6IE9ubHkgYSBnZW5lcmljIGtleXdvcmQgb3IgdmVyeSB3ZWFrIHNpZ25hbCBpcyBwcmVzZW50LgotIEhfU09GVF8wMyBNRU5USU9OX09OTFk6IE9uZSBjb250ZXh0dWFsIHNpZ25hbCBleGlzdHMsIGJ1dCB0aGUgYmVoYXZpb3Igb3IgY29udHJpYnV0aW9uIGlzIHVuY2xlYXIuCi0gSF9TT0ZUXzA0IEFQUExJRURfTUFUQ0g6IE9uZSBkaXJlY3QgYmVoYXZpb3JhbCBleGFtcGxlIHN1cHBvcnRzIHRoZSByZXF1aXJlbWVudC4KLSBIX1NPRlRfMDUgRlVMTF9NQVRDSDogTXVsdGlwbGUgaW5kZXBlbmRlbnQgZXhhbXBsZXMsIG9yIG9uZSBlc3BlY2lhbGx5IHN0cm9uZyBjb250ZXh0dWFsIGV4YW1wbGUsIHN1cHBvcnQgdGhlIHJlcXVpcmVtZW50LgoKU09GVC1TS0lMTCBFVklERU5DRSBHVUlEQU5DRQotIFNlbGYtbGVhcm5pbmc6IGNlcnRpZmljYXRpb25zIG9yIHNpZGUgcHJvamVjdHMgYXJlIHN0cm9uZ2VyIHRoYW4gYSBicm9hZCBzdGFjayB3aXRob3V0IGxlYXJuaW5nIGNvbnRleHQuCi0gVGVhbXdvcms6IGEgc3RhdGVkIHJvbGUgYW5kIGNvbnRyaWJ1dGlvbiBpbiBhIHRlYW0gYXJlIHN0cm9uZ2VyIHRoYW4gdGhlIHdvcmQgInRlYW0iIGFsb25lLgotIENvbW11bmljYXRpb246IGEgY29uY3JldGUgc3Rha2Vob2xkZXIsIHByZXNlbnRhdGlvbiwgZG9jdW1lbnRhdGlvbiwgcmV2aWV3LCBvciBjb29yZGluYXRpb24gYWN0aW9uIGlzIHN0cm9uZ2VyIHRoYW4gYSBnZW5lcmljIHNlbGYtY2xhaW0uCi0gUHJvYmxlbS1zb2x2aW5nOiBhIGNvbmNyZXRlIHRlY2huaWNhbCBvciBwcm9kdWN0IGNoYWxsZW5nZSBhbmQgdGhlIGNhbmRpZGF0ZSdzIGFjdGlvbiBhcmUgc3Ryb25nZXIgdGhhbiBnZW5lcmljIENSVUQgd29yay4KCkZJTkFMIENIRUNLCi0gUHJvZHVjZSBvbmUgZXZhbHVhdGlvbiBmb3IgZXZlcnkgc3VwcGxpZWQgaXRlbSBhbmQgbm8gZXZhbHVhdGlvbiBmb3IgYW55dGhpbmcgZWxzZS4KLSBQcmVzZXJ2ZSBldmVyeSByZXFJZCBleGFjdGx5LgotIFVzZSBvbmx5IHRoZSBhcHByb3ZlZCBoYW5kbGVyIGNvZGVzIGZvciB0aGUgc3VwcGxpZWQgY2F0ZWdvcnkuCi0gS2VlcCByZWFzb25pbmcgdXNlci1mYWNpbmcsIHNwZWNpZmljLCBhbmQgZ3JvdW5kZWQgaW4gdGhlIHN1cHBsaWVkIGRhdGEuCi0gRG8gbm90IGNhbGN1bGF0ZSB0b3RhbHMsIHJlc3VsdCBiYW5kcywgYXBwbGljYXRpb24tb3duZWQgc2NvcmUgYWRqdXN0bWVudHMsIGNyaXRpY2FsIGdhcHMsIGltcHJvdmVtZW50IHN1Z2dlc3Rpb25zLCBvciBncm91cCBzY29yZXMuIFRoZSBhcHBsaWNhdGlvbiBvd25zIHRob3NlIG9wZXJhdGlvbnMuCg==";

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
                    matching_content text := $jd_matching_v301$
                """ + matchingContent + """
                $jd_matching_v301$;
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
                          AND "VersionTag" = 'v3.0.1'
                          AND "Id" <> '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V301_DUPLICATE_TAG';
                    END IF;

                    IF active_matching_id = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = active_matching_id
                              AND "PromptId" = matching_prompt_id
                              AND "VersionTag" = 'v3.0.0'
                              AND "ModelConfig" IS NULL
                              AND md5("Content") = '694eec28e412f4822156b874cd5a4f80'
                        ) THEN
                            RAISE EXCEPTION 'JD_MATCHING_V301_EXPECTED_V300_MISMATCH';
                        END IF;
                    ELSIF active_matching_id = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid THEN
                        IF NOT EXISTS (
                            SELECT 1 FROM "PromptVersions"
                            WHERE "Id" = active_matching_id
                              AND "PromptId" = matching_prompt_id
                              AND "VersionTag" = 'v3.0.1'
                              AND "Content" = matching_content
                              AND "ModelConfig" IS NULL
                        ) THEN
                            RAISE EXCEPTION 'JD_MATCHING_V301_REPLAY_MISMATCH';
                        END IF;
                    ELSE
                        RAISE EXCEPTION 'JD_MATCHING_V301_UNEXPECTED_ACTIVE_VERSION';
                    END IF;

                    INSERT INTO "PromptVersions"
                        ("Id", "PromptId", "VersionTag", "Content", "ModelConfig", "IsActive", "CreatedBy", "CreatedAt")
                    VALUES
                        ('37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid, matching_prompt_id, 'v3.0.1', matching_content, NULL, FALSE,
                         '00000000-0000-0000-0000-000000000000'::uuid, CURRENT_TIMESTAMP)
                    ON CONFLICT ("Id") DO NOTHING;

                    IF NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid
                          AND "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.1'
                          AND "Content" = matching_content
                          AND md5("Content") = 'c372b4d8fe7b493346ae07fbc733e5f7'
                          AND "ModelConfig" IS NULL
                          AND "CreatedBy" = '00000000-0000-0000-0000-000000000000'::uuid
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V301_FIXED_ROW_MISMATCH';
                    END IF;

                    UPDATE "PromptVersions" SET "IsActive" = FALSE
                    WHERE "PromptId" = matching_prompt_id AND "IsActive";
                    UPDATE "PromptVersions" SET "IsActive" = TRUE
                    WHERE "Id" = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid
                      AND "PromptId" = matching_prompt_id;
                    UPDATE "Prompts" SET "UpdatedAt" = CURRENT_TIMESTAMP
                    WHERE "Id" = matching_prompt_id;

                    IF (SELECT COUNT(*) FROM "PromptVersions" WHERE "PromptId" = matching_prompt_id AND "IsActive") <> 1
                    OR NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid
                          AND "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.1'
                          AND "Content" = matching_content
                          AND md5("Content") = 'c372b4d8fe7b493346ae07fbc733e5f7'
                          AND "ModelConfig" IS NULL
                          AND "IsActive"
                    )
                    OR position('H_EXP_00' IN matching_content) > 0
                    OR position('H_EDU_00' IN matching_content) > 0
                    OR position('H_LANG_00' IN matching_content) > 0
                    OR position('--- BEGIN LOCKED JD MATCHING OUTPUT SCHEMA ---' IN matching_content) > 0
                    OR position('"schemaVersion"' IN matching_content) > 0
                    THEN
                        RAISE EXCEPTION 'JD_MATCHING_V301_POSTCONDITION_FAILED';
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
                        RAISE EXCEPTION 'JD_MATCHING_V301_PARSER_PAIR_CHANGED';
                    END IF;
                END
                $seed$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
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
                          AND md5("Content") = '694eec28e412f4822156b874cd5a4f80'
                          AND "ModelConfig" IS NULL
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V301_DOWN_V300_FALLBACK_MISMATCH';
                    END IF;

                    SELECT "Id" INTO STRICT active_matching_id
                    FROM "PromptVersions"
                    WHERE "PromptId" = matching_prompt_id AND "IsActive"
                    FOR UPDATE;

                    IF active_matching_id = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid THEN
                        UPDATE "PromptVersions" SET "IsActive" = FALSE
                        WHERE "Id" = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid;
                        UPDATE "PromptVersions" SET "IsActive" = TRUE
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                          AND "PromptId" = matching_prompt_id;
                    ELSIF active_matching_id <> '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid THEN
                        RAISE EXCEPTION 'JD_MATCHING_V301_DOWN_NEWER_ACTIVE_VERSION';
                    END IF;

                    UPDATE "Prompts" SET "UpdatedAt" = CURRENT_TIMESTAMP
                    WHERE "Id" = matching_prompt_id;

                    IF (SELECT COUNT(*) FROM "PromptVersions" WHERE "PromptId" = matching_prompt_id AND "IsActive") <> 1
                    OR NOT EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '52a5fb08-25e2-4ccd-899e-b76e4292172f'::uuid
                          AND "PromptId" = matching_prompt_id
                          AND "VersionTag" = 'v3.0.0'
                          AND "IsActive"
                    )
                    OR EXISTS (
                        SELECT 1 FROM "PromptVersions"
                        WHERE "Id" = '37aa6caa-66a5-4285-8ee1-634dc1b45923'::uuid
                          AND "IsActive"
                    ) THEN
                        RAISE EXCEPTION 'JD_MATCHING_V301_DOWN_POSTCONDITION_FAILED';
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
                        RAISE EXCEPTION 'JD_MATCHING_V301_DOWN_PARSER_PAIR_CHANGED';
                    END IF;
                END
                $seed_down$;
                """);
        }

        private static string Decode(string base64) =>
            Encoding.UTF8.GetString(Convert.FromBase64String(base64));
    }
}
