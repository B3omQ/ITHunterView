// d:\Desktop\FU\do an\Capstone Project\frontend\src\types\coin.types.ts

export interface CoinPackageDto {
  id: string;
  name: string;
  coins: number;
  price: number;
  isActive: boolean;
}

export interface CoinFeatureCostsDto {
  cvJdMatching: number;
  mockInterview: number;
  learningPath: number;
  unlockCv: number;
  postJob: number;
  extendJob: number;
  pushTop: number;
}

export interface CoinConfigResponseDto {
  featureCosts: CoinFeatureCostsDto;
  packages: CoinPackageDto[];
}
