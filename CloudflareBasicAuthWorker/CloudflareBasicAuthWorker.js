/**
 * Shows how to restrict access using the HTTP "Basic" schema.
 * @see https://developer.mozilla.org/en-US/docs/Web/HTTP/Authentication
 * @see https://tools.ietf.org/html/rfc7617
 *
 * A user-id containing a colon (":") character is invalid, as the
 * first colon in a user-pass string separates user and password.
 * 
 * credentials = auth-scheme 1*SP token68
 * auth-scheme = "Basic" ; case insensitive
 * token68     = 1*( ALPHA / DIGIT / "-" / "." / "_" / "~" / "+" / "/" ) *"="
 * 
 * This is a combination of:
 * https://github.com/dommmel/cloudflare-workers-basic-auth
 *  which itself credits https://github.com/jshttp/basic-auth
 * with code and ideas from https://developers.cloudflare.com/workers/examples/basic-auth
 */

const NAME = "admin"
const PASS = "admin"

const CREDENTIALS_REGEXP = /^ *(?:[Bb][Aa][Ss][Ii][Cc]) +([A-Za-z0-9._~+/-]+=*) *$/

/**
 * RegExp for basic auth user/pass
 *
 * user-pass   = userid ":" password
 * userid      = *<TEXT excluding ":">
 * password    = *TEXT
 */

const USER_PASS_REGEXP = /^([^:]*):(.*)$/

/**
 * Object to represent user credentials.
 */

const Credentials = function(name, pass) {
  this.name = name
  this.pass = pass
}

/**
 * Parse basic auth to object.
 */

const parseAuthHeader = function(string) {
  if (typeof string !== 'string') {
    return undefined
  }

  // parse header
  const match = CREDENTIALS_REGEXP.exec(string)

  if (!match) {
    return undefined
  }

  // decode user pass
  const userPass = USER_PASS_REGEXP.exec(atob(match[1]))

  if (!userPass) {
    return undefined
  }

  // return credentials object
  return new Credentials(userPass[1], userPass[2])
}


const unauthorizedResponse = function(body) {
  return new Response(
    body, {
      status: 401,
      headers: {
        "WWW-Authenticate": 'Basic realm="User Visible Realm"'
      }
    }
  )
}

/**
 * Handle request
 */

async function handle(request) {

  const { protocol, pathname } = new URL(request.url)

  // In the case of a "Basic" authentication, the exchange 
  // MUST happen over an HTTPS (TLS) connection to be secure.
  if ('https:' !== protocol || 'https' !== request.headers.get('x-forwarded-proto')) {
      throw new BadRequestException('Please use a HTTPS connection.')
  }

  if('/logout' === pathname)
    // Invalidate the "Authorization" header by returning a HTTP 401.
    // We do not send a "WWW-Authenticate" header, as this would trigger
    // a popup in the browser, immediately asking for credentials again.
    return new Response('Logged out.', { status: 401 })
      
  const credentials = parseAuthHeader(request.headers.get("Authorization"))
  if ( !credentials || credentials.name !== NAME ||  credentials.pass !== PASS) {
    return unauthorizedResponse("Unauthorized")
  } else {
    return fetch(request)
  }
}

addEventListener('fetch', event => {
  event.respondWith(handle(event.request))
})