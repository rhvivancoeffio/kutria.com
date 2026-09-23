/** Must match server default <see cref="TemplateProject.Application.Identity.PartnerIdentityConstants.SignupCookieName" /> */
export const PARTNER_SIGNUP_COOKIE_NAME = 'channels_partner_signup'

/** Best-effort clear of non-HttpOnly duplicate; server clears HttpOnly on signup/provision. */
export function clearPartnerSignupCookieClient() {
  try {
    document.cookie = `${PARTNER_SIGNUP_COOKIE_NAME}=; Path=/; Max-Age=0; SameSite=Lax`
  } catch (_) {}
}
