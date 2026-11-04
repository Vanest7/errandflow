export interface AuthMeResponse {
  externalUserId: string;
  localUserId: string | null;
  isRegistered: boolean;
}