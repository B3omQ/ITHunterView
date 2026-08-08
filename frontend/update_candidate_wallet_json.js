const fs = require('fs');
const enPath = 'messages/en.json';
const viPath = 'messages/vi.json';

const en = JSON.parse(fs.readFileSync(enPath, 'utf8'));
const vi = JSON.parse(fs.readFileSync(viPath, 'utf8'));

// Initialize namespaces if they don't exist
if (!en['CandidatePricing']) en['CandidatePricing'] = {};
if (!vi['CandidatePricing']) vi['CandidatePricing'] = {};
if (!en['CandidateTopUp']) en['CandidateTopUp'] = {};
if (!vi['CandidateTopUp']) vi['CandidateTopUp'] = {};
if (!en['CandidateBillingHistory']) en['CandidateBillingHistory'] = {};
if (!vi['CandidateBillingHistory']) vi['CandidateBillingHistory'] = {};

// CandidatePricing
en['CandidatePricing']['title'] = "Upgrade Your Application Experience";
vi['CandidatePricing']['title'] = "Nâng Cấp Trải Nghiệm Ứng Tuyển";

en['CandidatePricing']['subtitle'] = "Unlock powerful AI features to optimize your CV, practice interviews, and land your dream job faster. Start for free, upgrade when you're ready.";
vi['CandidatePricing']['subtitle'] = "Mở khóa các tính năng AI mạnh mẽ để tối ưu CV, luyện phỏng vấn và giành được công việc mơ ước nhanh hơn. Bắt đầu miễn phí, nâng cấp khi bạn sẵn sàng.";

en['CandidatePricing']['descBasic'] = "Start with basic features. Experience the system.";
vi['CandidatePricing']['descBasic'] = "Bắt đầu với các tính năng cơ bản. Trải nghiệm hệ thống.";

en['CandidatePricing']['descPro'] = "Everything you need to quickly land your dream job.";
vi['CandidatePricing']['descPro'] = "Mọi thứ bạn cần để nhanh chóng có được công việc mơ ước.";

en['CandidatePricing']['descMastery'] = "Advanced tools for professionals to maximize opportunities.";
vi['CandidatePricing']['descMastery'] = "Công cụ nâng cao cho chuyên gia để tối đa hóa cơ hội.";

en['CandidatePricing']['currency'] = "VND";
vi['CandidatePricing']['currency'] = "VNĐ";

en['CandidatePricing']['durationDays'] = "/{days} days";
vi['CandidatePricing']['durationDays'] = "/{days} ngày";

en['CandidatePricing']['cvMatch'] = "{limit} CV-JD Matches";
vi['CandidatePricing']['cvMatch'] = "{limit} Lượt đối chiếu CV-JD";

en['CandidatePricing']['cvOptimize'] = "{limit} CV Optimizations";
vi['CandidatePricing']['cvOptimize'] = "{limit} Lượt tối ưu CV";

en['CandidatePricing']['mockInterview'] = "{limit} AI Mock Interviews";
vi['CandidatePricing']['mockInterview'] = "{limit} Lượt phỏng vấn AI";

en['CandidatePricing']['learningPathSingle'] = "{limit} Learning Path creation (once per cycle)";
vi['CandidatePricing']['learningPathSingle'] = "{limit} Lượt tạo Learning Path (duy nhất trong chu kỳ)";

en['CandidatePricing']['learningPathMonthly'] = "{limit} Learning Path creations / month";
vi['CandidatePricing']['learningPathMonthly'] = "{limit} Lượt tạo Learning Path / tháng";

en['CandidatePricing']['learningPathSlotUnlimited'] = "Unlimited Learning Path storage slots";
vi['CandidatePricing']['learningPathSlotUnlimited'] = "Vô hạn Slot lưu trữ lộ trình học";

en['CandidatePricing']['learningPathSlot'] = "{limit} Learning Path storage slots";
vi['CandidatePricing']['learningPathSlot'] = "{limit} Slot lưu trữ lộ trình học";

en['CandidatePricing']['includesCoins'] = "Includes {coins} Coins";
vi['CandidatePricing']['includesCoins'] = "Bao gồm {coins} Coins";

en['CandidatePricing']['btnStart'] = "Start Using";
vi['CandidatePricing']['btnStart'] = "Bắt Đầu Sử Dụng";

en['CandidatePricing']['btnBuyNow'] = "Buy Now";
vi['CandidatePricing']['btnBuyNow'] = "Mua Ngay";

en['CandidatePricing']['btnUpgrade'] = "Upgrade";
vi['CandidatePricing']['btnUpgrade'] = "Nâng Cấp";

en['CandidatePricing']['noPlans'] = "There are currently no subscription plans available.";
vi['CandidatePricing']['noPlans'] = "Hiện tại chưa có gói dịch vụ nào.";


// CandidateTopUp
en['CandidateTopUp']['title'] = "Top Up Coins";
vi['CandidateTopUp']['title'] = "Nạp Thêm Coins";

en['CandidateTopUp']['subtitle'] = "Buy more coins to use advanced AI features like CV Matching and Mock Interviews on the platform.";
vi['CandidateTopUp']['subtitle'] = "Mua thêm Coins để sử dụng các tính năng AI nâng cao như Đối chiếu CV và Phỏng vấn thử trên nền tảng.";

en['CandidateTopUp']['currentBalance'] = "Your current balance:";
vi['CandidateTopUp']['currentBalance'] = "Số dư hiện tại của bạn:";

en['CandidateTopUp']['customCoinTitle'] = "Top Up Custom Coins";
vi['CandidateTopUp']['customCoinTitle'] = "Nạp Số Lượng Coins Tùy Chọn";

en['CandidateTopUp']['customCoinDesc'] = "Choose the exact number of Coins you need. This price is separate from Coin packages.";
vi['CandidateTopUp']['customCoinDesc'] = "Chọn số lượng Coins chính xác bạn cần. Mức giá này tách biệt với các gói Coins cố định.";

en['CandidateTopUp']['loadingPrice'] = "Loading custom Coin price...";
vi['CandidateTopUp']['loadingPrice'] = "Đang tải giá trị Coin tùy chọn...";

en['CandidateTopUp']['coinsToTopUp'] = "Coins to top up";
vi['CandidateTopUp']['coinsToTopUp'] = "Số Coins muốn nạp";

en['CandidateTopUp']['unitPrice'] = "Unit price";
vi['CandidateTopUp']['unitPrice'] = "Đơn giá";

en['CandidateTopUp']['unitPriceValue'] = "{price} / Coin";
vi['CandidateTopUp']['unitPriceValue'] = "{price} / Coin";

en['CandidateTopUp']['totalPayment'] = "Total payment";
vi['CandidateTopUp']['totalPayment'] = "Tổng thanh toán";

en['CandidateTopUp']['unavailable'] = "Custom Coin pricing is temporarily unavailable.";
vi['CandidateTopUp']['unavailable'] = "Giá trị Coin tùy chọn đang tạm thời không khả dụng.";

en['CandidateTopUp']['payForCustom'] = "Pay for custom Coins";
vi['CandidateTopUp']['payForCustom'] = "Thanh toán nạp Coins tùy chọn";

en['CandidateTopUp']['errInvalidAmount'] = "Please enter a whole number of Coins from 1 to 100,000.";
vi['CandidateTopUp']['errInvalidAmount'] = "Vui lòng nhập số nguyên Coins từ 1 đến 100.000.";

en['CandidateTopUp']['errCheckoutLink'] = "Checkout link not found";
vi['CandidateTopUp']['errCheckoutLink'] = "Không tìm thấy đường dẫn thanh toán";

en['CandidateTopUp']['errPayment'] = "An error occurred while creating payment";
vi['CandidateTopUp']['errPayment'] = "Đã xảy ra lỗi khi tạo thanh toán";

en['CandidateTopUp']['noPackages'] = "There are currently no coin packages available.";
vi['CandidateTopUp']['noPackages'] = "Hiện tại không có gói nạp Coins nào.";

en['CandidateTopUp']['tagPopular'] = "Popular";
vi['CandidateTopUp']['tagPopular'] = "Phổ biến";

en['CandidateTopUp']['descBeginner'] = "For beginners";
vi['CandidateTopUp']['descBeginner'] = "Dành cho người mới";

en['CandidateTopUp']['descValue'] = "Best value";
vi['CandidateTopUp']['descValue'] = "Tiết kiệm nhất";

en['CandidateTopUp']['descPro'] = "For professionals";
vi['CandidateTopUp']['descPro'] = "Dành cho chuyên gia";

en['CandidateTopUp']['btnBuyNow'] = "Buy Now";
vi['CandidateTopUp']['btnBuyNow'] = "Mua Ngay";

en['CandidateTopUp']['refTitle'] = "Wondering how much things cost?";
vi['CandidateTopUp']['refTitle'] = "Bạn muốn biết chi phí sử dụng các tính năng?";

en['CandidateTopUp']['refCvMatch'] = "CV Match:";
vi['CandidateTopUp']['refCvMatch'] = "Đối chiếu CV:";

en['CandidateTopUp']['refMockInterview'] = "Mock Interview:";
vi['CandidateTopUp']['refMockInterview'] = "Phỏng vấn AI:";

en['CandidateTopUp']['refLearningPath'] = "Learning Path:";
vi['CandidateTopUp']['refLearningPath'] = "Lộ trình học:";

// CandidateBillingHistory
en['CandidateBillingHistory']['title'] = "Transaction History";
vi['CandidateBillingHistory']['title'] = "Lịch Sử Giao Dịch";

en['CandidateBillingHistory']['subtitle'] = "Track your Subscription and Coin Top-up payments.";
vi['CandidateBillingHistory']['subtitle'] = "Theo dõi các thanh toán Gói dịch vụ và Nạp Coins của bạn.";

en['CandidateBillingHistory']['pageTitle'] = "Transaction History | ITHunterview";
vi['CandidateBillingHistory']['pageTitle'] = "Lịch sử giao dịch | ITHunterview";


fs.writeFileSync(enPath, JSON.stringify(en, null, 2));
fs.writeFileSync(viPath, JSON.stringify(vi, null, 2));

console.log('Candidate wallet translations updated successfully!');
