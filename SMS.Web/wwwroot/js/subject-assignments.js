(() => {
    const form = document.getElementById('subjectAssignmentForm');
    const counter = document.getElementById('subjectsCount');
    if (!form || !counter) return;
    const update = () => {
        counter.textContent = `${form.querySelectorAll('input[name="SelectedSubjectIds"]:checked').length} selected`;
    };
    form.addEventListener('change', update);
    update();
})();
