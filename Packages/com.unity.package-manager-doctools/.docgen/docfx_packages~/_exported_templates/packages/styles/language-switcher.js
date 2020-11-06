var langList = {
  en: "English",
  cn: "中文",
  ja: "日本語",
  es: "Español",
  kr: "한국어",
  ru: "Русский"
}

$(function () {

  var langListKeys = Object.keys(langList);
  var baseUrl = location.pathname;
  var currentLang = getCurrentLangauge();
  var languageSwitcher = ["<div id='language-switcher' style='display: none;'>", "<label for='language-select'>Language: <select id='language-select'>", "</select></label>", "</div>"]

  function existsLanguageDocs(url, className, lang) {
    $.ajax({
      url: url,
      method: "HEAD",
      statusCode: {
        404: function () {
          $(`.${className}`).prop('disabled', true);
        },
      },
    })
    .success(function() {
      if (lang !== 'en' && $('#language-switcher').is(":hidden")) {
        $('#language-switcher').show();
      }
    });
  }

  function getCurrentLangauge() {
    var metaLang = thisPackageMetaData.lang;

    for (var lang of Object.keys(langList)) {
      if (location.pathname.startsWith("/" + lang + "/Packages/")) {
        return lang;
      }
    }

    return metaLang;
  }


  $('#breadcrumb').append($(languageSwitcher.join("\n"))); // Create language switcher select box
  
  if (isOffline) { // If offline don't need to do anything else
    $('#language-switcher #language-select').prop('disabled', true).addClass("offline");
    return;
  }

  // reset baseUrl. /ja/Packages -> /Packages
  for (var lang of langListKeys) {
    baseUrl = baseUrl.replace(lang + "/Packages/" + thisPackageMetaData.name, "Packages/" + thisPackageMetaData.name);
  }

  for (var lang of langListKeys) {
    var relativeUrl = baseUrl;
    if (lang !== "en") {
      relativeUrl = `/${lang + baseUrl}`;
    }
    var selectedOption = currentLang === lang ? "selected" : "";

    var className = `language-switcher-language-${lang}`;
    $("#language-select").append(`<option class="${className}" value="${relativeUrl}" ${selectedOption}>${langList[lang]}</li>`);
    if (!isOffline) {
      existsLanguageDocs(relativeUrl, className, lang);
    }
  }

  $('#language-switcher #language-select').change(function () {
    location.href = $(this).val();
  });
  localStorage.setItem("docs-lang", currentLang);
});