
(() => {
    const searchInput = document.getElementById("roomSearch");
    const statusFilter = document.getElementById("roomStatusFilter");
    const typeFilter = document.getElementById("roomTypeFilter");
    const modal = document.getElementById("roomDetailModal");

    if (!searchInput || !statusFilter || !typeFilter || !modal) {
        return;
    }

    const cards = [...document.querySelectorAll(".room-card")];
    const floors = [...document.querySelectorAll(".room-floor")];

    const normalize = value => (value || "")
        .normalize("NFD")
        .replace(/[\u0300-\u036f]/g, "")
        .toLowerCase()
        .trim();

    function filterRooms() {
        const keyword = normalize(searchInput.value);
        const status = statusFilter.value;
        const type = normalize(typeFilter.value);

        cards.forEach(card => {
            const text = normalize(
                card.dataset.roomNumber + " " + card.dataset.roomType
            );

            const matchesKeyword = text.includes(keyword);
            const matchesStatus =
                !status || card.dataset.roomStatus === status;
            const matchesType =
                !type || normalize(card.dataset.roomType) === type;

            card.classList.toggle(
                "is-hidden",
                !(matchesKeyword && matchesStatus && matchesType)
            );
        });

        floors.forEach(floor => {
            const visibleCards = floor.querySelectorAll(
                ".room-card:not(.is-hidden)"
            );

            floor.classList.toggle(
                "is-hidden",
                visibleCards.length === 0
            );
        });
    }

    searchInput.addEventListener("input", filterRooms);
    statusFilter.addEventListener("change", filterRooms);
    typeFilter.addEventListener("change", filterRooms);

    function openRoomModal(card) {
        document.getElementById("modalRoomNumber").textContent =
            "Phòng " + card.dataset.roomNumber;

        document.getElementById("modalRoomType").textContent =
            card.dataset.roomType || "Chưa phân loại";

        document.getElementById("modalRoomPrice").textContent =
            Number(card.dataset.roomPrice || 0).toLocaleString("vi-VN") +
            " đ/đêm";

        document.getElementById("modalRoomNote").textContent =
            card.dataset.roomNote || "Chưa có ghi chú";

        const statusLabel = document.getElementById("modalRoomStatus");
        statusLabel.textContent = card.dataset.roomStatusName || "Chưa xác định";

        statusLabel.className = "room-status-label";

        const statusClasses = {
            Trong: "room-empty",
            DaDat: "room-booked",
            DangSuDung: "room-occupied",
            DangDon: "room-cleaning"
        };

        const statusClass = statusClasses[card.dataset.roomStatus];
        if (statusClass) {
            statusLabel.classList.add(statusClass);
        }

        // Chua co chuc nang sua phong: tam thoi an nut chinh sua
        document.getElementById("modalEditLink").hidden = true;

        modal.classList.add("is-open");
        modal.setAttribute("aria-hidden", "false");
        document.body.style.overflow = "hidden";
    }

    function closeRoomModal() {
        modal.classList.remove("is-open");
        modal.setAttribute("aria-hidden", "true");
        document.body.style.overflow = "";
    }

    cards.forEach(card => {
        card.addEventListener("click", () => openRoomModal(card));
    });

    modal.querySelectorAll("[data-close-modal]").forEach(button => {
        button.addEventListener("click", closeRoomModal);
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && modal.classList.contains("is-open")) {
            closeRoomModal();
        }
    });
})();
