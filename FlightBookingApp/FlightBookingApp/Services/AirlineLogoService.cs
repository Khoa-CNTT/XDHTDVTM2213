using FlightBookingApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FlightBookingApp.Services
{
    public class AirlineLogoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AirlineLogoService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _airhexApiKey;
        private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy;

        private readonly Dictionary<string, string> _iataCodes = new Dictionary<string, string>
{
    { "21 Air", "2I" },
    { "247 Aviation", null }, // Không tìm thấy mã IATA
    { "7Air Cargo", null }, // Không tìm thấy mã IATA
    { "9 Air Co", "AQ" },
    { "Abakan Air", null }, // Không tìm thấy mã IATA
    { "ABS Jets", null }, // Không tìm thấy mã IATA
    { "ABX Air", "GB" },
    { "ACE Air Cargo", null }, // Không tìm thấy mã IATA
    { "Advanced Air", "AN" },
    { "Aegean Airlines", "A3" },
    { "Aer Lingus", "EI" },
    { "Aerius Management", null }, // Không tìm thấy mã IATA
    { "Aero", null }, // Không tìm thấy mã IATA
    { "Aero Contractors", "NG" },
    { "Aero Dili", null }, // Không tìm thấy mã IATA
    { "Aero K", "RF" },
    { "Aero Mongolia", "M0" },
    { "Aero Nomad", null }, // Không tìm thấy mã IATA
    { "Aero Sotravia", null }, // Không tìm thấy mã IATA
    { "Aero-Beta Flight Training", null }, // Không tìm thấy mã IATA
    { "Aero-Dienst", null }, // Không tìm thấy mã IATA
    { "Aeroflot", "SU" },
    { "AeroGuard Flight Training Center", null }, // Không tìm thấy mã IATA
    { "Aeroitalia", "XZ" },
    { "Aerolineas Argentinas", "AR" },
    { "Aerolineas Ejecutivas", null }, // Không tìm thấy mã IATA
    { "Aerolineas Sosa", "S0" },
    { "Aerolink Uganda", null }, // Không tìm thấy mã IATA
    { "AeroLogic", "3S" },
    { "AeroLynx", null }, // Không tìm thấy mã IATA
    { "Aeromexico", "AM" },
    { "AeroMexico Connect", "5D" },
    { "Aeronaves Dominicanas", null }, // Không tìm thấy mã IATA
    { "Aeronaves TSM", null }, // Không tìm thấy mã IATA
    { "Aeropartner", null }, // Không tìm thấy mã IATA
    { "Aeroregional", "6G" },
    { "Aerosafin", null }, // Không tìm thấy mã IATA
    { "AeroUnion", "6R" },
    { "Aerovias DAP", null }, // Không tìm thấy mã IATA
    { "Aeroways", null }, // Không tìm thấy mã IATA
    { "Aerus", null }, // Không tìm thấy mã IATA
    { "Africa Airlines", null }, // Không tìm thấy mã IATA
    { "Africa Charter Airline", null }, // Không tìm thấy mã IATA
    { "Africa World Airlines", "AW" },
    { "African Express Airways", "M4" },
    { "Afrijet", "J7" },
    { "Afriqiyah Airways", "8U" },
    { "Aims Community College Aviation", null }, // Không tìm thấy mã IATA
    { "Air Adelphi", null }, // Không tìm thấy mã IATA
    { "Air Albania", "ZB" },
    { "Air Algerie", "AH" },
    { "Air Alsie", null }, // Không tìm thấy mã IATA
    { "Air Anka", null }, // Không tìm thấy mã IATA
    { "Air Antilles", "3S" },
    { "Air Arabia", "G9" },
    { "Air Arabia Abu Dhabi", "3L" },
    { "Air Arabia Egypt", "E5" },
    { "Air Arabia Maroc", "3O" },
    { "Air Armenia", "QN" }, // Hãng đã ngừng hoạt động
    { "Air Astana", "KC" },
    { "Air Astra", "2A" },
    { "Air Atlanta Europe", null }, // Không tìm thấy mã IATA
    { "Air Atlanta Icelandic", "CC" },
    { "Air Austral", "UU" },
    { "Air Baltic", "BT" },
    { "Air Belgium", "KF" },
    { "Air Botswana", "BP" },
    { "Air Burkina", "2J" },
    { "Air Busan", "BX" },
    { "Air Cairo", "SM" },
    { "Air Caledonie", "TY" },
    { "Air Canada", "AC" },
    { "Air Canada Rouge", "RV" },
    { "Air Caraibes", "TX" },
    { "Air Cargo Carriers", "2Q" },
    { "Air Century", "Y2" },
    { "Air Changan", "9H" },
    { "Air Charter Scotland", null }, // Không tìm thấy mã IATA
    { "Air Charters Europe", null }, // Không tìm thấy mã IATA
    { "Air Chathams", "CV" },
    { "Air China Cargo", "CA" },
    { "Air China Inner Mongolia", null }, // Không tìm thấy mã IATA
    { "Air China LTD", "CA" },
    { "Air Corsica", "XK" },
    { "Air Cote D'Ivoire", "HF" },
    { "Air Creebec", "YN" },
    { "Air Do", "HD" },
    { "Air Dolomiti", "EN" },
    { "Air Europa", "UX" },
    { "Air Excel", null }, // Không tìm thấy mã IATA
    { "Air Explore", "ED" },
    { "Air Flamenco", "F4" },
    { "Air France", "AF" },
    { "Air Georgian", "ZX" }, // Hãng đã ngừng hoạt động
    { "Air Greenland", "GL" },
    { "Air Guilin", "GT" },
    { "Air Hong Kong", "LD" },
    { "Air Horizont", null }, // Không tìm thấy mã IATA
    { "Air Incheon", "KJ" },
    { "Air India", "AI" },
    { "Air India Express", "IX" },
    { "Air Inuit", "3H" },
    { "Air Japan", "NQ" },
    { "Air KBZ", "K7" },
    { "Air Key West", null }, // Không tìm thấy mã IATA
    { "Air Koryo", "JS" },
    { "Air Liaison", null }, // Không tìm thấy mã IATA
    { "Air Loyaute", null }, // Không tìm thấy mã IATA
    { "Air Macau", "NX" },
    { "Air Malta", "KM" },
    { "Air Mauritius", "MK" },
    { "Air Moana", "NM" },
    { "Air Montenegro", "MNE" },
    { "Air New Zealand", "NZ" },
    { "Air Niugini", "PX" },
    { "Air North", "4N" },
    { "Air Ocean Maroc", null }, // Không tìm thấy mã IATA
    { "Air Panama", "7P" },
    { "Air Peace", "P4" },
    { "Air Premia", "YP" },
    { "Air Rarotonga", "GZ" },
    { "Air Samarkand", "SM" },
    { "Air Senegal", "HC" },
    { "Air Seoul", "RS" },
    { "Air Serbia", "JU" },
    { "Air Seychelles", "HM" },
    { "Air Sunshine", "YI" },
    { "Air Tahiti", "VT" },
    { "Air Tahiti Nui", "TN" },
    { "Air Tanzania", "TC" },
    { "Air Thanlwin", "ST" },
    { "Air Tindi", "8T" },
    { "Air Transat", "TS" },
    { "Air Transport", null }, // Không tìm thấy mã IATA
    { "Air Travel", "A6" },
    { "Air Vanuatu", "NF" },
    { "Air Wisconsin", "ZW" },
    { "Air Zimbabwe", "UM" },
    { "AirACT", null }, // Không tìm thấy mã IATA
    { "AirAsia", "AK" },
    { "AirAsia Cambodia", "KT" },
    { "AirAsia X", "D7" },
    { "Airblue", "PA" },
    { "Airbus Transport International", null }, // Không tìm thấy mã IATA
    { "Aircalin", "SB" },
    { "Aircharters Worldwide", null }, // Không tìm thấy mã IATA
    { "Aircraft Management Group", null }, // Không tìm thấy mã IATA
    { "Airest", null }, // Không tìm thấy mã IATA
    { "AirGO Private Airline", null }, // Không tìm thấy mã IATA
    { "airHaifa", "H9" },
    { "Airkenya", "P2" },
    { "Airlec Air Espace", null }, // Không tìm thấy mã IATA
    { "Airlink", "4Z" },
    { "AirNet", null }, // Không tìm thấy mã IATA
    { "Airnorth", "TL" },
    { "AirSial", "PF" },
    { "AirSmart", null }, // Không tìm thấy mã IATA
    { "Airsprint", null }, // Không tìm thấy mã IATA
    { "AirSWIFT", "T6" },
    { "AirX", null }, // Không tìm thấy mã IATA
    { "AIS Airlines", "IS" },
    { "Aitheras Aviation Group", null }, // Không tìm thấy mã IATA
    { "AJet", "VF" },
    { "Akasa Air", "QP" },
    { "Aklak Air", null }, // Không tìm thấy mã IATA
    { "Alante Air Charter", null }, // Không tìm thấy mã IATA
    { "Alaska Air Cargo", "AS" },
    { "Alaska Airlines", "AS" },
    { "Alaska Airlines (Boeing 100 years strong Livery)", "AS" },
    { "Alaska Airlines (Disney Toon Town Livery)", "AS" },
    { "Alaska Airlines (Disneyland - Pixar Pier Livery)", "AS" },
    { "Alaska Airlines (Honoring Those Who Serve Livery)", "AS" },
    { "Alaska Airlines (Oneworld Livery)", "AS" },
    { "Alaska Airlines (San Francisco Giants Livery)", "AS" },
    { "Alaska Airlines (Seattle Kraken Livery)", "AS" },
    { "Alaska Airlines (Seattle Mariners Livery)", "AS" },
    { "Alaska Airlines (Star Wars Livery)", "AS" },
    { "Alaska Airlines (UNCF Livery)", "AS" },
    { "Alaska Airlines (West Coast Wonders Livery)", "AS" },
    { "Alaska Airlines (Xáat Kwáani Livery)", "AS" },
    { "Alaska Horizon", "AS" },
    { "Alaska Horizon (Honoring Those Who Serve Livery)", "AS" },
    { "Alaska Horizon (Horizon Retro Livery)", "AS" },
    { "Alaska Horizon (University of Washington Livery)", "AS" },
    { "Alaska Horizon (Washington State Cougars Livery)", "AS" },
    { "Alaska SkyWest", "AS" },
    { "AlbaStar", "AP" },
    { "Albatros Airlines", null }, // Không tìm thấy mã IATA
    { "Alfa Airlines", null }, // Không tìm thấy mã IATA
    { "AlisCargo Airlines", null }, // Không tìm thấy mã IATA
    { "Aliserio", null }, // Không tìm thấy mã IATA
    { "Alitalia", "AZ" }, // Hãng đã ngừng hoạt động
    { "Alkebulan Airlines", null }, // Không tìm thấy mã IATA
    { "Allegiant Air", "G4" },
    { "Alliance Air", "9I" },
    { "Alliance Airlines", "QQ" },
    { "Alliance Executive Jets", null }, // Không tìm thấy mã IATA
    { "Allied Air", "4W" },
    { "AlMasria", "UJ" },
    { "Aloha Air Cargo", "KH" },
    { "Alpha Aviation", null }, // Không tìm thấy mã IATA
    { "AlphaSky", null }, // Không tìm thấy mã IATA
    { "Alpine Air Express", "5A" },
    { "Altagna", null }, // Không tìm thấy mã IATA
    { "Amakusa Air", null }, // Không tìm thấy mã IATA
    { "Amapola", "HP" },
    { "AMC Aviation", null }, // Không tìm thấy mã IATA
    { "Amelia", null }, // Không tìm thấy mã IATA
    { "American Air Charter", null }, // Không tìm thấy mã IATA
    { "American Airlines", "AA" },
    { "American Jet International", null }, // Không tìm thấy mã IATA
    { "Ameriflight", "A8" },
    { "Amerijet International", "M6" },
    { "Ameristar Jet Charter", null }, // Không tìm thấy mã IATA
    { "ANA", "NH" },
    { "Anderson Air", null }, // Không tìm thấy mã IATA
    { "Angara Airlines", "2G" },
    { "Anguilla Air Services", null }, // Không tìm thấy mã IATA
    { "Animawings", "A2" },
    { "Antonov Airlines", null }, // Không tìm thấy mã IATA
    { "APG Airlines", "GP" },
    { "Arcus Air", null }, // Không tìm thấy mã IATA
    { "Ariana Afghan Airlines", "FG" },
    { "Arik Air", "W3" },
    { "Arkia Israeli Airlines", "IZ" },
    { "Armenia Airways", "6A" },
    { "Armenian Airlines", null }, // Không tìm thấy mã IATA
    { "Ascend Airways", null }, // Không tìm thấy mã IATA
    { "ASG Business Aviation", null }, // Không tìm thấy mã IATA
    { "Asia Pacific Airlines", "P9" },
    { "Asiana Airlines", "OZ" },
    { "ASKY", "KP" },
    { "ASL Airlines Belgium", "3V" },
    { "ASL Airlines France", "5O" },
    { "ASL Airlines Ireland", "AG" },
    { "Asman Airlines", null }, // Không tìm thấy mã IATA
    { "Astonjet", null }, // Không tìm thấy mã IATA
    { "Astral Aviation", "8V" },
    { "ATA Airlines", "I3" },
    { "ATI", null }, // Không tìm thấy mã IATA
    { "Atlanta Air Charter", null }, // Không tìm thấy mã IATA
    { "Atlantic Airways", "RC" },
    { "Atlantic Flight Training Academy", null }, // Không tìm thấy mã IATA
    { "Atlas Air", "5Y" },
    { "Atran", null }, // Không tìm thấy mã IATA
    { "ATSA", null }, // Không tìm thấy mã IATA
    { "Auric Air", "UI" },
    { "Aurigny Air Services", "GR" },
    { "Aurora", "HZ" },
    { "Austrian", "OS" },
    { "AVA Airlines", null }, // Không tìm thấy mã IATA
    { "Avanti Air", "AT" },
    { "AvCenter", null }, // Không tìm thấy mã IATA
    { "Avcon Jet", null }, // Không tìm thấy mã IATA
    { "Avelo Airlines", "XP" },
    { "Avia Traffic Company", "YK" },
    { "Avianca Cargo", "QT" },
    { "Aviastar-TU", null }, // Không tìm thấy mã IATA
    { "Aviation Advisor", null }, // Không tìm thấy mã IATA
    { "Avincis", null }, // Không tìm thấy mã IATA
    { "Avion Express Malta", "X9" },
    { "Avionord", null }, // Không tìm thấy mã IATA
    { "Avior", "9V" },
    { "Aviostart", null }, // Không tìm thấy mã IATA
    { "Awesome Cargo", null }, // Không tìm thấy mã IATA
    { "AZAL Azerbaijan Airlines", "J2" },
    { "Azimuth", "A4" },
    { "Azores Airlines", "S4" },
    { "Aztec Airways", null }, // Không tìm thấy mã IATA
    { "Azul", "AD" },
    { "Azur Air", "ZF" },
    { "Badr Airlines", "J4" },
    { "Bahamasair", "UP" },
    { "Baker Aviation", null }, // Không tìm thấy mã IATA
    { "Bamboo Airways", "QH" },
    { "Bangkok Airways", "PG" },
    { "Baron Aviation Services", null }, // Không tìm thấy mã IATA
    { "Bartolini Air", null }, // Không tìm thấy mã IATA
    { "Base Kft", null }, // Không tìm thấy mã IATA
    { "Batik Air", "ID" },
    { "BBN Airlines", null }, // Không tìm thấy mã IATA
    { "BBN Airlines Indonesia", null }, // Không tìm thấy mã IATA
    { "Bearskin Airlines", "JV" },
    { "Beijing Airlines", null }, // Không tìm thấy mã IATA
    { "Beijing Capital Airlines", "JD" },
    { "Belavia", "B2" },
    { "BellAir", null }, // Không tìm thấy mã IATA
    { "Bemidji Aviation", null }, // Không tìm thấy mã IATA
    { "beOnd", "B4" },
    { "Bering Air", "8E" },
    { "Berjaya Air", "J8" },
    { "BermudAir", "2B" },
    { "Berniq Airways", null }, // Không tìm thấy mã IATA
    { "Berry Aviation", null }, // Không tìm thấy mã IATA
    { "Bestfly Aruba", null }, // Không tìm thấy mã IATA
    { "Bhutan Airlines", "B3" },
    { "Biman Bangladesh Airlines", "BG" },
    { "BinAir", null }, // Không tìm thấy mã IATA
    { "Binter Canarias", "NT" },
    { "Blue Bird Airways", "BZ" },
    { "Blue Dart Aviation", "BZ" },
    { "Blue Islands", "SI" },
    { "BlueSky Aviation", null }, // Không tìm thấy mã IATA
    { "BoA", "OB" },
    { "Boeing", null }, // Không phải hãng bay
    { "Boutique Air", "4B" },
    { "Braathens International Airways", null }, // Không tìm thấy mã IATA
    { "Braathens Regional", "DC" },
    { "Braathens Regional Airlines", "TF" },
    { "Breeze Airways", "MX" },
    { "British Airways", "BA" },
    { "Brussels Airlines", "SN" },
    { "Buddha Air", "U4" },
    { "Bul Air", "LB" },
    { "Bulgaria Air", "FB" },
    { "Bulgarian Air Charter", "1B" },
    { "Buraq Air", "UZ" },
    { "Business Wings", null }, // Không tìm thấy mã IATA
    { "C.A.L. Cargo Airlines", null }, // Không tìm thấy mã IATA
    { "CAA", "BU" },
    { "CAE", null }, // Không phải hãng bay
    { "Caicos Express Airways", "9Q" },
    { "Calm Air International", "MO" },
    { "Calstar", null }, // Không tìm thấy mã IATA
    { "Camair-Co", "QC" },
    { "Cambodia Airways", "KR" },
    { "Cambodia Angkor Air", "K6" },
    { "Canada - Royal Canadian Air Force", null }, // Không phải hãng bay
    { "Canadian Airways Congo", null }, // Không tìm thấy mã IATA
    { "Canadian North", "5T" },
    { "Canaryfly", "PM" },
    { "Cape Air", "9K" },
    { "CareFlight", null }, // Không tìm thấy mã IATA
    { "Cargo Air", "CG" },
    { "Cargojet", "W8" },
    { "Cargolux", "CV" },
    { "Cargolux Italia", "C8" },
    { "Caribbean Airlines", "BW" },
    { "Carpatair", "V3" },
    { "Caspian Airlines", "RV" },
    { "Castle Aviation", null }, // Không tìm thấy mã IATA
    { "Cathay Pacific", "CX" },
    { "Catreus", null }, // Không tìm thấy mã IATA
    { "Cavok Air", null }, // Không tìm thấy mã IATA
    { "Cayman Airways", "KX" },
    { "CB SkyShare", null }, // Không tìm thấy mã IATA
    { "Cebgo", "DG" },
    { "Cebu Pacific Air", "5J" },
    { "CemAir", "5Z" },
    { "Central Airlines", "I9" },
    { "Central Mountain Air", "9M" },
    { "Chabahar Airlines", null }, // Không tìm thấy mã IATA
    { "Chair Airlines", "GM" },
    { "Chalair", "CE" },
    { "Challenge Air Cargo", null }, // Không tìm thấy mã IATA
    { "Challenge Airlines BE", null }, // Không tìm thấy mã IATA
    { "Cham Wings Airlines", "6Q" },
    { "Charter Jets", null }, // Không tìm thấy mã IATA
    { "Chartright Air", null }, // Không tìm thấy mã IATA
    { "CHC Helicopter", null }, // Không phải hãng bay
    { "Chengdu Airlines", "EU" },
    { "China Airlines", "CI" },
    { "China Cargo", "CK" },
    { "China Eastern Airlines", "MU" },
    { "China Express Air", "G5" },
    { "China Postal Airlines", "CF" },
    { "China Southern Airlines", "CZ" },
    { "China Southern Cargo", "CZ" },
    { "China United Airlines", "KN" },
    { "Chongqing Airlines", "OQ" },
    { "Chrono Jet", null }, // Không tìm thấy mã IATA
    { "Cinnamon Air", "C7" },
    { "Citilink", "QG" },
    { "Cityjet", "WX" },
    { "ClipperJet", null }, // Không tìm thấy mã IATA
    { "CM Airlines", "CC" },
    { "CMA CGM Air Cargo", "2C" },
    { "Coastal Aviation", null }, // Không tìm thấy mã IATA
    { "Colorful Guizhou Airlines", "GY" },
    { "CommutAir", "C5" },
    { "Compass Air Cargo", "HQ" },
    { "Condor", "DE" },
    { "Congo Airways", "8Z" },
    { "ConocoPhillips", null }, // Không phải hãng bay
    { "Contour Aviation", "LF" },
    { "Conviasa", "V0" },
    { "Copa Airlines", "CM" },
    { "Copa Airlines Colombia", "P5" },
    { "Copenhagen Airtaxi", null }, // Không tìm thấy mã IATA
    { "Corendon Air", "XC" },
    { "Corporate Air", null }, // Không tìm thấy mã IATA
    { "Corsair", "SS" },
    { "Costa Rica Green Airways", null }, // Không tìm thấy mã IATA
    { "Croatia Airlines", "OU" },
    { "Cronos Airlines", "C8" },
    { "Cross Aviation", null }, // Không tìm thấy mã IATA
    { "Crown Airlines", null }, // Không tìm thấy mã IATA
    { "CSA Air", null }, // Không tìm thấy mã IATA
    { "CSI Aviation", null }, // Không tìm thấy mã IATA
    { "CSM Aviation", null }, // Không tìm thấy mã IATA
    { "Cubana de Aviacion", "CU" },
    { "Cygnus Air", "Y2" },
    { "Cyprus Airways", "CY" },
    { "Czechia - Air Force", null }, // Không phải hãng bay
    { "Daallo Airlines", "D3" },
    { "Dalian Airlines", "CA" },
    { "Dan Air", "DN" },
    { "Danish Air", null }, // Không tìm thấy mã IATA
    { "DAS Private Jets", null }, // Không tìm thấy mã IATA
    { "De Havilland Canada", null }, // Không phải hãng bay
    { "Delta Air Lines", "DL" },
    { "Denver Air Connection", "KG" },
    { "DesertAir Alaska", null }, // Không tìm thấy mã IATA
    { "Dexter Air Taxi", null }, // Không tìm thấy mã IATA
    { "DHL Aero Expreso", "D5" },
    { "DHL Air", "D0" },
    { "Directflight Limited", null }, // Không tìm thấy mã IATA
    { "Discover Airlines", "4Y" },
    { "Divi Divi Air", "3R" },
    { "Donghai Airlines", "DZ" },
    { "Draken Europe", null }, // Không tìm thấy mã IATA
    { "Dreamline Aviation", null }, // Không tìm thấy mã IATA
    { "DRF Luftrettung", null }, // Không phải hãng bay
    { "Dronamics Europe Airlines", null }, // Không tìm thấy mã IATA
    { "Drukair", "KB" },
    { "Eagle Air", "H7" },
    { "EagleMed", null }, // Không tìm thấy mã IATA
    { "East Coast Jets", null }, // Không tìm thấy mã IATA
    { "EASTAR JET", "ZE" },
    { "Eastern Air Express", null }, // Không tìm thấy mã IATA
    { "Eastern Airways", "T3" },
    { "Easy Charter", null }, // Không tìm thấy mã IATA
    { "EasyFly", "VE" },
    { "easyJet", "U2" },
    { "EAT Leipzig", "QY" },
    { "E-Aviation", null }, // Không tìm thấy mã IATA
    { "EcoJet", "8J" },
    { "Edelweiss Air", "WK" },
    { "EgyptAir", "MS" },
    { "EgyptAir Cargo", "MS" },
    { "El Al", "LY" },
    { "Electra Airways", "3E" },
    { "Embry-Riddle Aeronautical University", null }, // Không phải hãng bay
    { "Emirates", "EK" },
    { "Empire Airlines", "EM" },
    { "empty", null }, // Không phải hãng bay
    { "ENAC Ecole Aviation Civile", null }, // Không phải hãng bay
    { "Encore Air Cargo", null }, // Không tìm thấy mã IATA
    { "ENG Aviation", null }, // Không tìm thấy mã IATA
    { "Enter Air", "E4" },
    { "Estafeta Carga Aerea", "E7" },
    { "Estelar", "ES" },
    { "Eswatini Air", "SZ" },
    { "ETF Airways", "LI" },
    { "Ethiopian Airlines", "ET" },
    { "Etihad Airways", "EY" },
    { "Euro Link", null }, // Không tìm thấy mã IATA
    { "Euro-Asia Air", null }, // Không tìm thấy mã IATA
    { "EuroAtlantic Airways", "YU" },
    { "European Aircraft Private Club", null }, // Không tìm thấy mã IATA
    { "European Cargo", null }, // Không tìm thấy mã IATA
    { "Eurowings", "EW" },
    { "EVA Air", "BR" },
    { "Evelop Airlines", "E9" },
    { "Everts Air Cargo", "5V" },
    { "EWA Air", "ZD" },
    { "Excellent Air", null }, // Không tìm thấy mã IATA
    { "Exclusive Aviation", null }, // Không tìm thấy mã IATA
    { "Executive Aviation Services", null }, // Không tìm thấy mã IATA
    { "Executive Flight Services", null }, // Không tìm thấy mã IATA
    { "Executive Jet Management", null }, // Không tìm thấy mã IATA
    { "Express Air Cargo", "7T" },
    { "FAI rent-a-jet", null }, // Không tìm thấy mã IATA
    { "Fanjet Express", null }, // Không tìm thấy mã IATA
    { "Fastjet Zimbabwe", "FN" },
    { "FedEx", "FX" },
    { "Fenix Air Charter", null }, // Không tìm thấy mã IATA
    { "Fiji Airways", "FJ" },
    { "Finistair", null }, // Không tìm thấy mã IATA
    { "Finnair", "AY" },
    { "Firefly", "FY" },
    { "First Air", "7F" }, // Hãng đã ngừng hoạt động
    { "FitsAir", "8D" },
    { "Flair Airlines", "F8" },
    { "Fleet Air International", null }, // Không tìm thấy mã IATA
    { "FlexFlight", "W2" },
    { "Flexjet", null }, // Không tìm thấy mã IATA
    { "Flight Calibration Services", null }, // Không tìm thấy mã IATA
    { "Flight Options", null }, // Không tìm thấy mã IATA
    { "Flight Training Europe", null }, // Không tìm thấy mã IATA
    { "FlightExec", null }, // Không tìm thấy mã IATA
    { "Flightline", null }, // Không tìm thấy mã IATA
    { "Flightlink", null }, // Không tìm thấy mã IATA
    { "Flightpath Charter Airways", null }, // Không tìm thấy mã IATA
    { "Flightstar", null }, // Không tìm thấy mã IATA
    { "Fltplan", null }, // Không tìm thấy mã IATA
    { "Fly Air41 Airways", null }, // Không tìm thấy mã IATA
    { "Fly All Ways", "8W" },
    { "Fly Alliance", null }, // Không tìm thấy mã IATA
    { "Fly Angola", "EQ" },
    { "Fly Away", null }, // Không tìm thấy mã IATA
    { "Fly Baghdad", "IF" },
    { "Fly Jinnah", "9P" },
    { "Fly Khiva", null }, // Không tìm thấy mã IATA
    { "Fly One", "5F" },
    { "Fly OYA", null }, // Không tìm thấy mã IATA
    { "Fly Pro", null }, // Không tìm thấy mã IATA
    { "Fly2Sky", "F6" },
    { "Fly4 Airlines", null }, // Không tìm thấy mã IATA
    { "Fly540", "5H" },
    { "Fly91", "5P" },
    { "Flyadeal", "F3" },
    { "FlyArystan", "0Y" },
    { "FlyBig", "S9" },
    { "Flybondi", "FO" },
    { "Flycana", null }, // Không tìm thấy mã IATA
    { "Fly-Coop Air Service", null }, // Không tìm thấy mã IATA
    { "flydubai", "FZ" },
    { "flyExclusive", null }, // Không tìm thấy mã IATA
    { "FLYGTA Airlines", null }, // Không tìm thấy mã IATA
    { "Flylili", "FL" },
    { "Flyme", "VP" },
    { "FlyMontserrat", "5M" },
    { "FlyNamibia", "WV" },
    { "flynas", "XY" },
    { "FlyOne Armenia", "3F" },
    { "FlyPelican", null }, // Không tìm thấy mã IATA
    { "Flyyo", null }, // Không tìm thấy mã IATA
    { "Four Corners Aviation", null }, // Không tìm thấy mã IATA
    { "France - Air Forces Command", null }, // Không phải hãng bay
    { "Franconia Air Service", null }, // Không tìm thấy mã IATA
    { "Freebird Airlines", "FH" },
    { "Freedom Airline Express", null }, // Không tìm thấy mã IATA
    { "Freight Runners Express", null }, // Không tìm thấy mã IATA
    { "French Bee", "BF" },
    { "Frontier Airlines", "F9" },
    { "FROST", null }, // Không tìm thấy mã IATA
    { "Fuji Dream Airlines", "JH" },
    { "Fuzhou Airlines", "FU" },
    { "Galistair Infinite Aviation", "G8" },
    { "Gama Aviation", null }, // Không tìm thấy mã IATA
    { "Garuda Indonesia", "GA" },
    { "Genghis Khan Airlines", "9D" },
    { "Georgian Airlines", null }, // Không tìm thấy mã IATA
    { "Georgian Airways", "A9" },
    { "Geosky", null }, // Không tìm thấy mã IATA
    { "German Airways", "ZQ" },
    { "Germany - Air Force", null }, // Không phải hãng bay
    { "Germany - DLR Flugbetriebe", null }, // Không phải hãng bay
    { "Germany - Navy", null }, // Không phải hãng bay
    { "Gestair", null }, // Không tìm thấy mã IATA
    { "GetJet Airlines", "GW" },
    { "Ghadames Air Transport", null }, // Không tìm thấy mã IATA
    { "Global Air Charters", null }, // Không tìm thấy mã IATA
    { "Global Aviation Operations", null }, // Không tìm thấy mã IATA
    { "Global Reach Aviation", null }, // Không tìm thấy mã IATA
    { "GlobalX", "G6" },
    { "GlobeAir", null }, // Không tìm thấy mã IATA
    { "Go2Sky", "6G" },
    { "GoJet Airlines", "G7" },
    { "Gol", "G3" },
    { "GP Aviation", null }, // Không tìm thấy mã IATA
    { "Grafair", null }, // Không tìm thấy mã IATA
    { "Grand Canyon Airlines", null }, // Không tìm thấy mã IATA
    { "Grand China Air", "CN" },
    { "Grant Aviation", "GV" },
    { "Greater Bay Airlines", "HB" },
    { "Greece - Air Force", null }, // Không phải hãng bay
    { "Green Africa", "Q9" },
    { "Groupe Transair", null }, // Không tìm thấy mã IATA
    { "Gryphon Air", null }, // Không tìm thấy mã IATA
    { "Gryphon Airlines", null }, // Không tìm thấy mã IATA
    { "GTA Air", null }, // Không tìm thấy mã IATA
    { "Guangxi Beibu Gulf Airlines", "GP" },
    { "Gulf Air", "GF" },
    { "Gulf and Caribbean Cargo", null }, // Không tìm thấy mã IATA
    { "GullivAir", "G2" },
    { "Hai Au Aviation", null }, // Không tìm thấy mã IATA
    { "Hainan Airlines", "HU" },
    { "Harmony Jets", null }, // Không tìm thấy mã IATA
    { "Hawaiian Airlines", "HA" },
    { "Hayways", null }, // Không tìm thấy mã IATA
    { "Hebei Airlines", "NS" },
    { "Heli Air Monaco", null }, // Không tìm thấy mã IATA
    { "Helijet International", null }, // Không tìm thấy mã IATA
    { "HELITY Copter Airlines", null }, // Không tìm thấy mã IATA
    { "HelloJets", null }, // Không tìm thấy mã IATA
    { "Helvetic Airways", "2L" },
    { "Heston Airlines", null }, // Không tìm thấy mã IATA
    { "Hex'Air", "UD" },
    { "Hi Fly", "5K" },
    { "Hi Fly Malta", null }, // Không tìm thấy mã IATA
    { "Himalaya Airlines", "H9" },
    { "Hinterland Aviation", "OI" },
    { "HiSky", "H4" },
    { "HiSky Europe", null }, // Không tìm thấy mã IATA
    { "Hokkaido Air System", "6L" },
    { "Hong Kong Air Cargo", "RH" },
    { "Hong Kong Airlines", "HX" },
    { "Hong Kong Express", "UO" },
    { "HOP!", "A5" },
    { "Hop-A-Jet", null }, // Không tìm thấy mã IATA
    { "Hunnu Air", "MR" },
    { "IASCO Flight Training", null }, // Không tìm thấy mã IATA
    { "IBC Airways", "II" },
    { "Iberia", "IB" },
    { "Iberia Express", "I2" },
    { "IBEX Airlines", "FW" },
    { "Ibom Air", "QI" },
    { "Icelandair", "FI" },
    { "IFL Group", null }, // Không tìm thấy mã IATA
    { "I-Fly", null }, // Không tìm thấy mã IATA
    { "I-Fly Air", null }, // Không tìm thấy mã IATA
    { "IndiaOne Air", "G8" },
    { "IndiGo", "6E" },
    { "Indonesia AirAsia", "QZ" },
    { "interCaribbean Airways", "JY" },
    { "International Jet Management", null }, // Không tìm thấy mã IATA
    { "IrAero", "IO" },
    { "Iran Air", "IR" },
    { "Iran Airtour", "B9" },
    { "Iran Aseman Airlines", "EP" },
    { "Iraqi Airways", "IA" },
    { "Isles Of Scilly Skybus", null }, // Không tìm thấy mã IATA
    { "Israir Airlines", "6H" },
    { "ITA Airways", "AZ" },
    { "Italfly", null }, // Không tìm thấy mã IATA
    { "Italy - Air Force", null }, // Không phải hãng bay
    { "Italy - Army", null }, // Không phải hãng bay
    { "Izhavia", "I8" },
    { "J-Air", "XM" },
    { "J-Air (Expo 2025 Osaka Livery)", "XM" },
    { "Jambojet", "JM" },
    { "Japan Air Commuter", "JC" },
    { "Japan Airlines", "JL" },
    { "Japan Airlines (Airbus A350-1000 Red Livery)", "JL" },
    { "Japan Airlines (Disney 100 Livery)", "JL" },
    { "Japan Airlines (Dream Sho Jet Livery)", "JL" },
    { "Japan Airlines (Expo Osaka 2025 Livery)", "JL" },
    { "Japan Airlines (Oneworld Livery)", "JL" },
    { "Japan Airlines (Tokyo DisneySea Livery)", "JL" },
    { "Japan Transocean Air", "NU" },
    { "Jazeera Airways", "J9" },
    { "JDL Airlines", null }, // Không tìm thấy mã IATA
    { "Jeju Air", "7C" },
    { "Jet Aviation Business Jets", null }, // Không tìm thấy mã IATA
    { "Jet Charter", null }, // Không tìm thấy mã IATA
    { "Jet Fly Airline", null }, // Không tìm thấy mã IATA
    { "Jet Linx Aviation", null }, // Không tìm thấy mã IATA
    { "Jet Logistics", null }, // Không tìm thấy mã IATA
    { "Jet OUT", null }, // Không tìm thấy mã IATA
    { "Jet Story", null }, // Không tìm thấy mã IATA
    { "Jet2", "LS" },
    { "JetBlue Airways", "B6" },
    { "Jetfly Aviation", null }, // Không tìm thấy mã IATA
    { "JetNetherlands", null }, // Không tìm thấy mã IATA
    { "jetNEXA", null }, // Không tìm thấy mã IATA
    { "JetRight", null }, // Không tìm thấy mã IATA
    { "JetSMART", "JA" },
    { "Jetstar", "JQ" },
    { "Jetstar Asia", "3K" },
    { "Jetstar Japan", "GK" },
    { "JetStream", null }, // Không tìm thấy mã IATA
    { "Jettime", "JP" },
    { "Jetways Airlines Limited", null }, // Không tìm thấy mã IATA
    { "Jiangxi Airlines", "RY" },
    { "Jin Air", "LJ" },
    { "Jonair", null }, // Không tìm thấy mã IATA
    { "Jordan Aviation Airlines", "R5" },
    { "Journey Aviation", null }, // Không tìm thấy mã IATA
    { "Joy Air", "JR" },
    { "JSC Avion Express", "X9" },
    { "JSX Air", "XE" },
    { "Jubba Airways (Kenya)", "3J" },
    { "Juneyao Airlines", "HO" },
    { "Jung Sky", null }, // Không tìm thấy mã IATA
    { "Kaiser Air", null }, // Không tìm thấy mã IATA
    { "Kalitta Air", "K4" },
    { "Kalitta Charters", null }, // Không tìm thấy mã IATA
    { "Kalitta Charters II", null }, // Không tìm thấy mã IATA
    { "Kam Air", "RQ" },
    { "Kamaka Air", null }, // Không tìm thấy mã IATA
    { "Karun Airlines", null }, // Không tìm thấy mã IATA
    { "Kenai Aviation", null }, // Không tìm thấy mã IATA
    { "Kenmore Air", "M5" },
    { "Kenya Airways", "KQ" },
    { "KF Cargo", "FK" },
    { "KhabAvia", null }, // Không tìm thấy mã IATA
    { "KlasJet", null }, // Không tìm thấy mã IATA
    { "KLM", "KL" },
    { "KM Malta Airlines", "KM" },
    { "K-Mile Air", "8K" },
    { "Kolob Canyons Air Services", null }, // Không tìm thấy mã IATA
    { "Komiaviatrans", "KO" },
    { "Korean Air", "KE" },
    { "KrasAvia", "KI" },
    { "Kunming Airlines", "KY" },
    { "Kuwait Airways", "KU" },
    { "L.J. Aviation", null }, // Không tìm thấy mã IATA
    { "L3Harris Airline Academy", null }, // Không tìm thấy mã IATA
    { "La Compagnie", "B0" },
    { "La Costena", null }, // Không tìm thấy mã IATA
    { "LabCorp", null }, // Không phải hãng bay
    { "LAM", "TM" },
    { "Lanmei Airlines", "LQ" },
    { "Lao Airlines", "QV" },
    { "Lao Skyway", "LK" },
    { "LASER Airlines", "QL" },
    { "LATAM Airlines", "LA" },
    { "LATAM Cargo Brasil", "M3" },
    { "LATAM Cargo Chile", "UC" },
    { "LATAM Cargo Colombia", "L7" },
    { "Lauda Europe", "LW" },
    { "LEAV Aviation", null }, // Không tìm thấy mã IATA
    { "Legends Airways", null }, // Không tìm thấy mã IATA
    { "LeTourneau University Aviation", null }, // Không tìm thấy mã IATA
    { "Level", "LV" },
    { "Leviate Air Group", null }, // Không tìm thấy mã IATA
    { "LIAT 20", "LI" },
    { "Liberty Jet Management", null }, // Không tìm thấy mã IATA
    { "Libyan Airlines", "LN" },
    { "Libyan Wings", "YL" },
    { "Life Line Aviation", null }, // Không tìm thấy mã IATA
    { "Lion Air", "JT" },
    { "Loganair", "LM" },
    { "Longhao Airlines", "GI" },
    { "LongJiang Airlines", null }, // Không tìm thấy mã IATA
    { "Loong Air", "GJ" },
    { "LOT - Polish Airlines", "LO" },
    { "Lucky Air", "8L" },
    { "Lufthansa", "LH" },
    { "Lufthansa Cargo", "LH" },
    { "Lufthansa City", "CL" },
    { "Lufttransport", null }, // Không tìm thấy mã IATA
    { "Luminair", null }, // Không tìm thấy mã IATA
    { "Lumiwings", "L9" },
    { "Luxair", "LG" },
    { "Luxaviation Belgium", null }, // Không tìm thấy mã IATA
    { "Luxaviation UK", null }, // Không tìm thấy mã IATA
    { "Luxembourg Air Ambulance", null }, // Không tìm thấy mã IATA
    { "Luxwing", null }, // Không tìm thấy mã IATA
    { "Lynden Air Cargo", "L2" },
    { "Madagascar Airlines", "MD" },
    { "MAE Aircraft Management", null }, // Không tìm thấy mã IATA
    { "Magnicharters", "UJ" },
    { "Mahan Air", "W5" },
    { "Malawian Airlines", "3W" },
    { "Malaysia Airlines", "MH" },
    { "Maldivian", "Q2" },
    { "Malindo Air", "OD" },
    { "Malta Air", "AL" },
    { "Malta MedAir", "MT" },
    { "Mandarin Airlines", "AE" },
    { "Mann Yadanarpon Airlines", "7Y" },
    { "Marabu", "DI" },
    { "Marathon Airlines", null }, // Không tìm thấy mã IATA
    { "Martinair", "MP" },
    { "Martinaire", null }, // Không tìm thấy mã IATA
    { "Masair", null }, // Không tìm thấy mã IATA
    { "MASwings", "MY" },
    { "Mauritania Airlines International", "L6" },
    { "Mavi Gök Airlines", null }, // Không tìm thấy mã IATA
    { "Max Air", "VM" },
    { "Maxair", null }, // Không tìm thấy mã IATA
    { "Maya Island Air", "2M" },
    { "Medsky Airways", null }, // Không tìm thấy mã IATA
    { "Meraj Air", null }, // Không tìm thấy mã IATA
    { "Mesa Airlines", "YV" },
    { "Mexicana", "MX" },
    { "MHS Aviation", null }, // Không tìm thấy mã IATA
    { "Miat - Mongolian Airlines", "OM" },
    { "Middle East Airlines", "ME" },
    { "Minoan Air", null }, // Không tìm thấy mã IATA
    { "Mira Vista Aviation", null }, // Không tìm thấy mã IATA
    { "Mirny Air Enterprise", null }, // Không tìm thấy mã IATA
    { "Mistral Air", "M4" },
    { "MNG Airlines", "MB" },
    { "Moalem Aviation", null }, // Không tìm thấy mã IATA
    { "Moçambique Expresso", "MX" },
    { "Modern Logistics", null }, // Không tìm thấy mã IATA
    { "Mongolian Airways", null }, // Không tìm thấy mã IATA
    { "Motor Sich Airlines", "M9" },
    { "Mountain Air Cargo", "C2" },
    { "Mountain Aviation", null }, // Không tìm thấy mã IATA
    { "Mustang Aviation", null }, // Không tìm thấy mã IATA
    { "My Freighter", null }, // Không tìm thấy mã IATA
    { "My Indo Airlines", "2Y" },
    { "My Jet", null }, // Không tìm thấy mã IATA
    { "Myanmar Airways International", "8M" },
    { "Myanmar National Airlines", "UB" },
    { "Nam Air", "IN" },
    { "National Airlines", "N8" },
    { "National Jet Express", "J3" },
    { "National Jets", null }, // Không tìm thấy mã IATA
    { "NATO", null }, // Không phải hãng bay
    { "Nauru Airlines", "ON" },
    { "NCA - Nippon Cargo Airlines", "KZ" },
    { "NEAJETS", null }, // Không tìm thấy mã IATA
    { "Neos", "NO" },
    { "Nepal Airlines", "RA" },
    { "Nesma Airlines", "NE" },
    { "Netherlands - Royal Air Force", null }, // Không phải hãng bay
    { "NetJets Aviation", null }, // Không tìm thấy mã IATA
    { "NetJets Europe", null }, // Không tìm thấy mã IATA
    { "Netjets UK", null }, // Không tìm thấy mã IATA
    { "New England Airlines", "EJ" },
    { "New Way Cargo Airlines", null }, // Không tìm thấy mã IATA
    { "NexGen Aviation", null }, // Không tìm thấy mã IATA
    { "NextGen Flight Solutions", null }, // Không tìm thấy mã IATA
    { "NG Eagle", null }, // Không tìm thấy mã IATA
    { "Nile Air", "NP" },
    { "Nok Air", "DD" },
    { "Nolinor", "N5" },
    { "Noordzee Helikopters Vlaanderen", null }, // Không phải hãng bay
    { "NordStar Airlines", "Y7" },
    { "Nordwind Airlines", "N4" },
    { "Norlandair", null }, // Không tìm thấy mã IATA
    { "Norse", "N0" },
    { "Norse UK", null }, // Không tìm thấy mã IATA
    { "North Flying", null }, // Không tìm thấy mã IATA
    { "North Star Aviation", null }, // Không tìm thấy mã IATA
    { "Northern Air Cargo", "NC" },
    { "Northern Jet Management", null }, // Không tìm thấy mã IATA
    { "North-West Air Company", null }, // Không tìm thấy mã IATA
    { "Northwest Flyers", null }, // Không tìm thấy mã IATA
    { "North-Western Cargo International Airlines", null }, // Không tìm thấy mã IATA
    { "North-Wright Airways", "HW" },
    { "Norwegian", "DY" },
    { "Norwegian Air Sweden", "D8" },
    { "Nouvelair Tunisie", "BJ" },
    { "Novoair", "VQ" },
    { "Nyxair", null }, // Không tìm thấy mã IATA
    { "Okay Airways", "BK" },
    { "Olympic Air", "OA" },
    { "Oman Air", "WY" },
    { "Omni Air International", "OY" },
    { "Omni Air Transport", null }, // Không tìm thấy mã IATA
    { "Omni Aviation", null }, // Không tìm thấy mã IATA
    { "One Air", null }, // Không tìm thấy mã IATA
    { "Oriental Air Bridge", "OC" },
    { "Ortac", null }, // Không tìm thấy mã IATA
    { "Overland Airways", "OJ" },
    { "Paccair", null }, // Không tìm thấy mã IATA
    { "Pacific Coast Jet", null }, // Không tìm thấy mã IATA
    { "Pacific Coastal Airlines", "8P" },
    { "PADAviation", null }, // Không tìm thấy mã IATA
    { "Pakistan International Airlines", "PK" },
    { "PAL Airlines", "PB" },
    { "Pan American Airways", null }, // Không tìm thấy mã IATA
    { "Pan Europeenne", null }, // Không tìm thấy mã IATA
    { "Pan Europeenne Air Service", null }, // Không tìm thấy mã IATA
    { "Paranair", "ZP" },
    { "Parkland College Institute of Aviation", null }, // Không tìm thấy mã IATA
    { "Pascan Aviation", "P6" },
    { "PassionAir", "OP" },
    { "Patient Airlift Services", null }, // Không tìm thấy mã IATA
    { "Peach Aviation", "MM" },
    { "Pecotox Air", null }, // Không tìm thấy mã IATA
    { "Pegas Fly", "IK" },
    { "Pegasus", "PC" },
    { "Pegasus Elite Aviation", null }, // Không tìm thấy mã IATA
    { "Pelita Air", "IP" },
    { "Pen-Avia", null }, // Không tìm thấy mã IATA
    { "Peoples", "PE" },
    { "PHI", null }, // Không tìm thấy mã IATA
    { "Philippine Airlines", "PR" },
    { "Philippines AirAsia", "Z2" },
    { "Piedmont Airlines", "PT" },
    { "Pilot Flight Academy", null }, // Không tìm thấy mã IATA
    { "Pineapple Air", null }, // Không tìm thấy mã IATA
    { "Planemaster Services", null }, // Không tìm thấy mã IATA
    { "PlaneSense", null }, // Không tìm thấy mã IATA
    { "Platoon Aviation", null }, // Không tìm thấy mã IATA
    { "Plus Ultra", "PU" },
    { "PNG Air", "CG" },
    { "Pobeda", "DP" },
    { "Poland - Air Force", null }, // Không phải hãng bay
    { "Polar Air Cargo", "PO" },
    { "Polar Airlines", "PI" },
    { "Porter", "PD" },
    { "Porter Airlines", "PD" },
    { "Porter Airlines Canada", "PD" },
    { "Precision Air", "PW" },
    { "Presidential Aviation", null }, // Không tìm thấy mã IATA
    { "Priester Aviation", null }, // Không tìm thấy mã IATA
    { "Prince Aviation", null }, // Không tìm thấy mã IATA
    { "Private Jets", null }, // Không tìm thấy mã IATA
    { "Private owner", null }, // Không phải hãng bay
    { "Private Wings", null }, // Không tìm thấy mã IATA
    { "Privilege Style", "P6" },
    { "ProAir Aviation", null }, // Không tìm thấy mã IATA
    { "Proflight Zambia", "P0" },
    { "Propair", null }, // Không tìm thấy mã IATA
    { "PSA Airlines", "OH" },
    { "Qanot Sharq", "HH" },
    { "Qantas", "QF" },
    { "QantasLink", "QF" },
    { "Qatar Airways", "QR" },
    { "Qatar Executive", null }, // Không tìm thấy mã IATA
    { "Qazaq Air", "IQ" },
    { "Qeshm Airlines", "QB" },
    { "Qingdao Airlines", "QJ" },
    { "Quest Diagnostics", null }, // Không phải hãng bay
    { "Quick Air Jet Charter", null }, // Không tìm thấy mã IATA
    { "Quikjet Cargo Airlines", "QO" },
    { "RAF-Avia", null }, // Không tìm thấy mã IATA
    { "Rano Air", "R4" },
    { "Ravn Alaska", "7H" },
    { "Raya Airways", "TH" },
    { "RED Air", "L5" },
    { "Red Sea Airlines", null }, // Không tìm thấy mã IATA
    { "Red Wings", "WZ" },
    { "Redding Aero Enterprises", null }, // Không tìm thấy mã IATA
    { "Redstar Aviation", null }, // Không tìm thấy mã IATA
    { "Reeve Air Alaska", null }, // Không tìm thấy mã IATA
    { "Regency Air", null }, // Không tìm thấy mã IATA
    { "Regional Air", null }, // Không tìm thấy mã IATA
    { "Reliable Airlines", null }, // Không tìm thấy mã IATA
    { "Reliant Air", null }, // Không tìm thấy mã IATA
    { "Renegade Air", null }, // Không tìm thấy mã IATA
    { "Rennia Aviation", null }, // Không tìm thấy mã IATA
    { "Republic Airways", "YX" },
    { "Revv Aviation", null }, // Không tìm thấy mã IATA
    { "Rex Regional Express", "ZL" },
    { "Reynolds Jet Management", null }, // Không tìm thấy mã IATA
    { "Richland Aviation", null }, // Không tìm thấy mã IATA
    { "Romania - Air Force", null }, // Không phải hãng bay
    { "Rossiya Airlines", "FV" },
    { "Royal Air Charter", null }, // Không tìm thấy mã IATA
    { "Royal Air Maroc", "AT" },
    { "Royal Brunei Airlines", "BI" },
    { "Royal Jordanian", "RJ" },
    { "Royalair Philippines", null }, // Không tìm thấy mã IATA
    { "Ruili Airlines", "DR" },
    { "RUS Aviation", null }, // Không tìm thấy mã IATA
    { "RusLine", "7R" },
    { "Rutaca Airlines", "5R" },
    { "RVL Aviation", null }, // Không tìm thấy mã IATA
    { "RwandAir", "WB" },
    { "Ryan Air", null }, // Không tìm thấy mã IATA
    { "Ryanair", "FR" },
    { "Ryanair Sun", "RR" },
    { "Ryanair UK", "RK" },
    { "Ryukyu Air Commuter", "RAC" },
    { "S7 Airlines", "S7" },
    { "SA AVIANCA", "AV" },
    { "Safair", "FA" },
    { "Safarilink", "F2" },
    { "Saha Airlines", null }, // Không tìm thấy mã IATA
    { "Saint Barth Commuter", null }, // Không tìm thấy mã IATA
    { "SalamAir", "OV" },
    { "Samoa Airways", "OL" },
    { "San Marino Executive Aviation", null }, // Không tìm thấy mã IATA
    { "SANSA Regional", "RZ" },
    { "Sapsan Airline", null }, // Không tìm thấy mã IATA
    { "Sardinian Sky Service", null }, // Không tìm thấy mã IATA
    { "SAS", "SK" },
    { "SASCA Airlines", null }, // Không tìm thấy mã IATA
    { "SATA Air Acores", "SP" },
    { "SATENA", "9R" },
    { "Saudia", "SV" },
    { "SaxonAir", null }, // Không tìm thấy mã IATA
    { "SC Aviation", null }, // Không tìm thấy mã IATA
    { "Scanwings", null }, // Không tìm thấy mã IATA
    { "SCAT Airlines", "DV" },
    { "Scoot", "TR" },
    { "Seaborne Airlines", "BB" },
    { "Secure Air Charter", null }, // Không tìm thấy mã IATA
    { "Security Aviation", null }, // Không tìm thấy mã IATA
    { "Sepehran Airlines", "IS" },
    { "Serene Air", "ER" },
    { "Sevenair", null }, // Không tìm thấy mã IATA
    { "SevenBar Aviation", null }, // Không tìm thấy mã IATA
    { "Severstal Aircompany", "D2" },
    { "SF Airlines", "O3" },
    { "Shandong Airlines", "SC" },
    { "Shanghai Airlines", "FM" },
    { "Sharp Airlines", "SH" },
    { "Shenzhen Airlines", "ZH" },
    { "Shirak Avia", "5G" },
    { "Shuttle America", "S5" }, // Hãng đã ngừng hoạt động
    { "Sichuan Airlines", "3U" },
    { "Sierra Charlie Aviation", null }, // Không tìm thấy mã IATA
    { "Sierra Pacific Airlines", null }, // Không tìm thấy mã IATA
    { "Silk Way Airlines", "ZP" },
    { "Silk Way West", "7L" },
    { "Silkavia", null }, // Không tìm thấy mã IATA
    { "Silver Air", null }, // Không tìm thấy mã IATA
    { "Silver Airways", "3M" },
    { "Silver Cloud Air", null }, // Không tìm thấy mã IATA
    { "Silverhawk Aviation", null }, // Không tìm thấy mã IATA
    { "Singapore Airlines", "SQ" },
    { "Sky Airline", "H2" },
    { "Sky Airline Peru", "H8" },
    { "Sky Angkor", "ZA" },
    { "Sky Cana", "RD" },
    { "Sky Express", "GQ" },
    { "Sky Fru", null }, // Không tìm thấy mã IATA
    { "Sky Mali", "ML" },
    { "Sky Quest", null }, // Không tìm thấy mã IATA
    { "Sky Regional", "RS" }, // Hãng đã ngừng hoạt động
    { "Sky Vision Airlines", null }, // Không tìm thấy mã IATA
    { "Skyborne Airline Academy", null }, // Không tìm thấy mã IATA
    { "SKYhigh Dominicana", "DO" },
    { "SkyLine Express", null }, // Không tìm thấy mã IATA
    { "Skymark Airlines", "BC" },
    { "Skypower Express Airways", null }, // Không tìm thấy mã IATA
    { "Skyservice Business Aviation", null }, // Không tìm thấy mã IATA
    { "Skytrans", "QN" },
    { "SkyUp Airlines", "PQ" },
    { "Skyward Express", "OW" },
    { "SkyWest Airlines", "OO" },
    { "Slate Aviation", null }, // Không tìm thấy mã IATA
    { "Smart Jet", null }, // Không tìm thấy mã IATA
    { "Smartavia", "5N" },
    { "SmartLynx Airlines", "6Y" },
    { "SmartWings", "QS" },
    { "Smartwings Poland", "3Z" },
    { "Solairus Aviation", null }, // Không tìm thấy mã IATA
    { "Solaseed Air", "6J" },
    { "Solinair", null }, // Không tìm thấy mã IATA
    { "Solomon Airlines", "IE" },
    { "Somon Air", "SZ" },
    { "Sounds Air", "S8" },
    { "South African Airlink", "4Z" },
    { "South African Airways", "SA" },
    { "Southern Air Charter", null }, // Không tìm thấy mã IATA
    { "Southern Airways Express", "9X" },
    { "Southwest Airlines", "WN" },
    { "Southwind Airlines", "2S" },
    { "Spain - Air Force", null }, // Không phải hãng bay
    { "Spartan College of Aeronautics and Technology", null }, // Không tìm thấy mã IATA
    { "Specsavers Aviation", null }, // Không tìm thấy mã IATA
    { "SpiceJet", "SG" },
    { "Spirit Airlines", "NK" },
    { "Spring Airlines", "9C" },
    { "Spring Airlines Japan", "IJ" },
    { "Sprint Air", null }, // Không tìm thấy mã IATA
    { "SprintAir Cargo", null }, // Không tìm thấy mã IATA
    { "SriLankan Airlines", "UL" },
    { "Sriwijaya Air", "SJ" },
    { "St Barth Executive", null }, // Không tìm thấy mã IATA
    { "STAjets", null }, // Không tìm thấy mã IATA
    { "Star Air", "S5" },
    { "Star Peru", "2I" },
    { "StarFlyer", "7G" },
    { "Starlink Aviation", null }, // Không tìm thấy mã IATA
    { "Starlux", "JX" },
    { "STP Airways", "8F" },
    { "Su Airlines", null }, // Không tìm thấy mã IATA
    { "Suburban Air Freight", null }, // Không tìm thấy mã IATA
    { "Summit Aviation", null }, // Không tìm thấy mã IATA
    { "Sun Country Airlines", "SY" },
    { "Sunclass Airlines", "DK" },
    { "Sundair", "SR" },
    { "SunExpress", "XQ" },
    { "Sunrise Airways", "S6" },
    { "Sunwest Aviation", null }, // Không tìm thấy mã IATA
    { "Sunwing", "WG" },
    { "Suparna Airlines", "Y8" },
    { "Super Air Jet", "IU" },
    { "Surf Air", null }, // Không tìm thấy mã IATA
    { "Surinam Airways", "PY" },
    { "Susi Air", null }, // Không tìm thấy mã IATA
    { "Svenskt Ambulansflyg", null }, // Không tìm thấy mã IATA
    { "Swiftair", "WT" },
    { "Swiftair Hellas", null }, // Không tìm thấy mã IATA
    { "SWISS", "LX" },
    { "Swiss Air-Ambulance", null }, // Không tìm thấy mã IATA
    { "Swiss Global Air Lines", "LZ" },
    { "Syrian Air", "RB" },
    { "TAAG", "DT" },
    { "TACV", "VR" },
    { "TAG", null }, // Không tìm thấy mã IATA
    { "Tailwind Airlines", "TI" },
    { "Talon Air", null }, // Không tìm thấy mã IATA
    { "TAP Air Portugal", "TP" },
    { "Taquan Air", "K3" },
    { "TAR Aerolineas", "YQ" },
    { "Tarco Air", "3T" },
    { "TAROM", "RO" },
    { "Tassili Airlines", "SF" },
    { "Texel Air", null }, // Không tìm thấy mã IATA
    { "Tez Jet", "TE" },
    { "Thai AirAsia", "FD" },
    { "Thai AirAsia X", "XJ" },
    { "Thai Airways International", "TG" },
    { "Thai Lion Air", "SL" },
    { "Thai Vietjet Air", "VZ" },
    { "Thomas Cook Airlines Balearics", "H6" }, // Hãng đã ngừng hoạt động
    { "Thrive", null }, // Không tìm thấy mã IATA
    { "Thunder Airlines", null }, // Không tìm thấy mã IATA
    { "Tianjin Air Cargo", "HT" },
    { "Tianjin Airlines", "GS" },
    { "Tibet Airlines", "TV" },
    { "Tigerair Taiwan", "IT" },
    { "Time Air", null }, // Không tìm thấy mã IATA
    { "Titan Airways", "ZT" },
    { "Titan Airways Malta", null }, // Không tìm thấy mã IATA
    { "Toll Aviation", null }, // Không tìm thấy mã IATA
    { "Toyo Aviation", null }, // Không tìm thấy mã IATA
    { "Trade Air", "C3" },
    { "Tradewind Aviation", "TJ" },
    { "Trans Air Congo", "Q8" },
    { "Trans Guyana Airways", null }, // Không tìm thấy mã IATA
    { "Transair", null }, // Không tìm thấy mã IATA
    { "Transavia", "HV" },
    { "Transavia France", "TO" },
    { "TransNusa", "8B" },
    { "Transwest Air", null }, // Không tìm thấy mã IATA
    { "Trident Aircraft", null }, // Không tìm thấy mã IATA
    { "Trigana Air", "IL" },
    { "Tri-MG Intra Asia Airlines", "GM" },
    { "Tropic Air", "PM" },
    { "Tropic Ocean Airways", "TI" },
    { "TUI Airways", "BY" },
    { "TUI fly", "X3" },
    { "TUIfly", "X3" },
    { "TUIfly Netherlands", "OR" },
    { "Tunisair", "TU" },
    { "Tunisair Express", "UG" },
    { "Turkish Airlines", "TK" },
    { "Turkmenistan - Government", null }, // Không phải hãng bay
    { "Turkmenistan Airlines", "T5" },
    { "Turpial Airlines", "T9" },
    { "Tus Airways", "U8" },
    { "T'Way Air", "TW" },
    { "Twin Jet", "T7" },
    { "Tyrol Air Ambulance", null }, // Không tìm thấy mã IATA
    { "Tyrolean Jet Services", null }, // Không tìm thấy mã IATA
    { "Uganda Airlines", "UR" },
    { "ULS Airlines Cargo", "GO" },
    { "UND Aerospace", null }, // Không tìm thấy mã IATA
    { "UNI Air", "B7" },
    { "Unicair", null }, // Không tìm thấy mã IATA
    { "Union Aviation", null }, // Không tìm thấy mã IATA
    { "United Airlines", "UA" },
    { "United Kingdom - Air Ambulance", null }, // Không tìm thấy mã IATA
    { "United Nigeria Airlines", "U5" },
    { "Universal Air", "VO" },
    { "UPS Airlines", "5X" },
    { "UR Airlines", "UD" },
    { "Ural Airlines", "U6" },
    { "Urumqi Airlines", "UQ" },
    { "USA Jet Airlines", "UJ" },
    { "US-Bangla Airlines", "BS" },
    { "USC", null }, // Không tìm thấy mã IATA
    { "UTair Aviation", "UT" },
    { "UVT Aero", "RT" },
    { "Uzbekistan Airways", "HY" },
    { "Valair", null }, // Không tìm thấy mã IATA
    { "Valletta Airlines", null }, // Không tìm thấy mã IATA
    { "VallJet", null }, // Không tìm thấy mã IATA
    { "ValueJet", "VK" },
    { "Varesh Airlines", null }, // Không tìm thấy mã IATA
    { "VASCO", "0V" },
    { "Venezolana", "AW" },
    { "Ventura", null }, // Không tìm thấy mã IATA
    { "Venture Aviation Group", null }, // Không tìm thấy mã IATA
    { "ViaAir", null }, // Không tìm thấy mã IATA
    { "Vieques Air Link", "V4" },
    { "VietJet Air", "VJ" },
    { "Vietnam Airlines", "VN" },
    { "Vietravel Airlines", "VU" },
    { "Virgin Atlantic", "VS" },
    { "Virgin Australia", "VA" },
        { "Vista America", null }, // Không tìm thấy mã IATA
    { "VistaJet", null }, // Không tìm thấy mã IATA
    { "VivaAerobus", "VB" },
    { "VoePass", "2Z" },
    { "Volare Aviation", null }, // Không tìm thấy mã IATA
    { "Volaris", "Y4" },
    { "Volaris Costa Rica", "Q6" },
    { "Volotea", "V7" },
    { "Vueling", "VY" },
    { "Wamos Air", "EB" },
    { "Warbelow's Air Ventures", "4W" },
    { "Wasaya Airways", "WT" },
    { "WEJET", null }, // Không tìm thấy mã IATA
    { "West Air (China)", "PN" },
    { "West Air (USA)", null }, // Không tìm thấy mã IATA
    { "West Air Sweden", "PT" },
    { "West Atlantic UK", "NPT" },
    { "West Coast Charters", null }, // Không tìm thấy mã IATA
    { "Western Air", "WST" },
    { "Western Aircraft", null }, // Không tìm thấy mã IATA
    { "Western Global Airlines", "KD" },
    { "WestJet", "WS" },
    { "WestJet Encore", "WR" },
    { "Wheels Up", null }, // Không tìm thấy mã IATA
    { "Wideroe", "WF" },
    { "Wiggins Airways", "WIG" },
    { "Winair", "WM" },
    { "Wind Rose Aviation Company", "7W" },
    { "Wings Air (Indonesia)", "IW" },
    { "Wings West Airlines", null }, // Hãng đã ngừng hoạt động
    { "Wizz Air", "W6" },
    { "Wizz Air Abu Dhabi", "5W" },
    { "Wizz Air Malta", "W4" },
    { "Wizz Air UK", "W9" },
    { "World Atlantic Airlines", "WL" },
    { "World2Fly", "2W" },
    { "Worldwide Jet Charter", null }, // Không tìm thấy mã IATA
    { "Wright Air Service", "8V" },
    { "XE Jet", null }, // Không tìm thấy mã IATA
    { "Xiamen Airlines", "MF" },
    { "Xinjiang Skylink General Aviation", null }, // Không tìm thấy mã IATA
    { "Yakutia", "R3" },
    { "Yamal Airlines", "YL" },
    { "Yazd Airways", null }, // Không tìm thấy mã IATA
    { "Yemenia", "IY" },
    { "Yeti Airlines", "YT" },
    { "YTO Cargo Airlines", "YG" },
    { "Z Air", "7Z" },
    { "Zagros Airlines", "ZO" },
    { "Zambia Airways", "ZN" },
    { "ZanAir", "B4" },
    { "Zenflight", null }, // Không tìm thấy mã IATA
    { "Zimex Aviation", "XM" },
    { "Zimex Aviation Austria", null }, // Không tìm thấy mã IATA
    { "Zipair", "ZG" }
};

        public AirlineLogoService(
            ApplicationDbContext context,
            ILogger<AirlineLogoService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _airhexApiKey = configuration["Airhex:ApiKey"];

            _retryPolicy = Policy<HttpResponseMessage>
                .Handle<HttpRequestException>()
                .OrResult(r => r.StatusCode == System.Net.HttpStatusCode.TooManyRequests || !r.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (response, timespan, retryCount, context) =>
                    {
                        _logger.LogWarning($"Yêu cầu thất bại. Thử lại lần {retryCount} sau {timespan.TotalSeconds} giây.");
                    });
        }

        public async Task<(int updatedCount, int failedCount)> UpdateAirlineLogosAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Bắt đầu cập nhật logo cho các hãng bay. ForceUpdate: {ForceUpdate}", forceUpdate);

                var testUrl = GenerateAirhexLogoUrl("VN", "r", 350, 100, _airhexApiKey);
                _logger.LogInformation($"Kiểm tra Airhex API với URL: {testUrl}");
                var client = _httpClientFactory.CreateClient();
                var testResponse = await client.GetAsync(testUrl, cancellationToken);
                if (!testResponse.IsSuccessStatusCode)
                {
                    _logger.LogError("Airhex API không khả dụng. Mã trạng thái: {StatusCode}", testResponse.StatusCode);
                    throw new Exception("Airhex API không khả dụng.");
                }
                _logger.LogInformation("Airhex API hoạt động bình thường.");

                const int batchSize = 100;
                int skip = 0;
                int updatedCount = 0;
                int failedCount = 0;

                while (true)
                {
                    _logger.LogInformation($"Lấy lô hãng bay (skip: {skip}, batchSize: {batchSize}).");
                    var query = _context.Airlines.AsQueryable();
                    if (!forceUpdate)
                    {
                        query = query.Where(a => string.IsNullOrEmpty(a.LogoUrl) || a.LogoUrl == "/images/default-airline-logo.png");
                    }
                    var batch = await query
                        .Skip(skip)
                        .Take(batchSize)
                        .ToListAsync(cancellationToken);

                    if (!batch.Any())
                    {
                        _logger.LogInformation("Không còn hãng bay nào để cập nhật logo.");
                        break;
                    }

                    _logger.LogInformation($"Lô hiện tại có {batch.Count} hãng bay.");
                    foreach (var airline in batch)
                    {
                        try
                        {
                            _logger.LogInformation($"Xử lý hãng bay: {airline.Name} (ID: {airline.AirlineId}).");

                            // Kiểm tra URL hiện tại (nếu có)
                            if (!string.IsNullOrEmpty(airline.LogoUrl) && airline.LogoUrl != "/images/default-airline-logo.png")
                            {
                                _logger.LogInformation($"Kiểm tra URL hiện tại: {airline.LogoUrl}");
                                var urlCheckResponse = await client.GetAsync(airline.LogoUrl, cancellationToken);
                                if (urlCheckResponse.IsSuccessStatusCode)
                                {
                                    _logger.LogInformation($"URL hiện tại của {airline.Name} vẫn hợp lệ. Bỏ qua cập nhật.");
                                    continue; // Bỏ qua nếu URL hiện tại vẫn hợp lệ
                                }
                                _logger.LogWarning($"URL hiện tại của {airline.Name} không hợp lệ. Mã trạng thái: {urlCheckResponse.StatusCode}. Tiến hành cập nhật.");
                            }

                            string iataCode = null;
                            if (!string.IsNullOrEmpty(airline.IataCode))
                            {
                                iataCode = airline.IataCode;
                                _logger.LogInformation($"Mã IATA từ database: {iataCode}.");
                            }
                            else if (_iataCodes.TryGetValue(airline.Name, out string code))
                            {
                                iataCode = code;
                                _logger.LogInformation($"Mã IATA từ dictionary: {iataCode}.");
                            }
                            else
                            {
                                _logger.LogWarning($"Không tìm thấy mã IATA cho hãng bay: {airline.Name}.");
                            }

                            if (iataCode != null)
                            {
                                string logoUrl = GenerateAirhexLogoUrl(iataCode, "r", 350, 100, _airhexApiKey);
                                _logger.LogInformation($"URL logo được tạo: {logoUrl}");

                                var response = await _retryPolicy.ExecuteAsync(() => client.GetAsync(logoUrl, cancellationToken));
                                if (response.IsSuccessStatusCode)
                                {
                                    airline.LogoUrl = logoUrl;
                                    _logger.LogInformation($"Gán LogoUrl cho {airline.Name}: {logoUrl}");
                                }
                                else
                                {
                                    airline.LogoUrl = "/images/default-airline-logo.png";
                                    failedCount++;
                                    _logger.LogWarning($"Sử dụng logo mặc định cho {airline.Name} (IATA: {iataCode}). Mã trạng thái: {response.StatusCode}");
                                }
                            }
                            else
                            {
                                airline.LogoUrl = "/images/default-airline-logo.png";
                                failedCount++;
                                _logger.LogWarning($"Sử dụng logo mặc định cho {airline.Name} do không có mã IATA.");
                            }

                            _context.Airlines.Update(airline);
                            _logger.LogInformation($"Đã cập nhật entity cho {airline.Name} trong context.");
                            updatedCount++;
                        }
                        catch (Exception ex)
                        {
                            failedCount++;
                            _logger.LogError(ex, $"Lỗi khi cập nhật logo cho {airline.Name}: {ex.Message}");
                        }
                    }

                    _logger.LogInformation($"Lưu thay đổi vào database cho lô hiện tại (skip: {skip}).");
                    int changesSaved = await _context.SaveChangesAsync(cancellationToken);
                    _logger.LogInformation($"Đã lưu {changesSaved} thay đổi vào database.");

                    skip += batchSize;
                }

                _logger.LogInformation($"Hoàn tất cập nhật logo. Thành công: {updatedCount}, Thất bại: {failedCount}.");
                return (updatedCount, failedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật logo cho các hãng bay.");
                throw;
            }
        }

        private string GenerateAirhexLogoUrl(string iataCode, string type, int width, int height, string apiKey)
        {
            string hashString = $"{iataCode}_{width}_{height}_{type}_{apiKey}";
            using (var md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(hashString);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }
                string md5Hash = sb.ToString();
                return $"https://content.airhex.com/content/logos/airlines_{iataCode}_{width}_{height}_{type}.png?md5apikey={md5Hash}";
            }
        }
    }
}