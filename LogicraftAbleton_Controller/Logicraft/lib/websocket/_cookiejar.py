try:
    import http.cookies
except:
    import http.cookies as Cookie


class SimpleCookieJar(object):
    def __init__(self):
        self.jar = dict()

    def add(self, set_cookie):
        if set_cookie:
            try:
                simpleCookie = http.cookies.SimpleCookie(set_cookie)
            except:
                simpleCookie = http.cookies.SimpleCookie(set_cookie.encode('ascii', 'ignore'))

            for k, v in list(simpleCookie.items()):
                domain = v.get("domain")
                if domain:
                    if not domain.startswith("."):
                        domain = "." + domain
                    cookie = self.jar.get(domain) if self.jar.get(domain) else http.cookies.SimpleCookie()
                    cookie.update(simpleCookie)
                    self.jar[domain.lower()] = cookie

    def set(self, set_cookie):
        if set_cookie:
            try:
                simpleCookie = http.cookies.SimpleCookie(set_cookie)
            except:
                simpleCookie = http.cookies.SimpleCookie(set_cookie.encode('ascii', 'ignore'))

            for k, v in list(simpleCookie.items()):
                domain = v.get("domain")
                if domain:
                    if not domain.startswith("."):
                        domain = "." + domain
                    self.jar[domain.lower()] = simpleCookie

    def get(self, host):
        if not host:
            return ""

        cookies = []
        for domain, simpleCookie in list(self.jar.items()):
            host = host.lower()
            if host.endswith(domain) or host == domain[1:]:
                cookies.append(self.jar.get(domain))

        return "; ".join([_f for _f in ["%s=%s" % (k, v.value) for cookie in [_f for _f in sorted(cookies) if _f] for k, v in
                                       sorted(cookie.items())] if _f])
