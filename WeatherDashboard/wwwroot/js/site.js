document.addEventListener("DOMContentLoaded", function () {

    const searchInput =
        document.getElementById("city");

    const searchForm =
        document.querySelector(".weather-search");

    if (!searchInput || !searchForm) {
        return;
    }


    // ==========================================
    // CLEAN SEARCH
    // ==========================================

    searchForm.addEventListener(
        "submit",
        function () {

            const value =
                searchInput.value.trim();

            searchInput.value = value;

        }
    );


    // ==========================================
    // ENTER KEY
    // ==========================================

    searchInput.addEventListener(
        "keydown",
        function (event) {

            if (event.key === "Enter") {

                event.preventDefault();

                searchForm.submit();

            }

        }
    );


    // ==========================================
    // AUTO FOCUS
    // ==========================================

    if (
        window.innerWidth > 600 &&
        searchInput.value.length === 0
    ) {
        searchInput.focus();
    }

});